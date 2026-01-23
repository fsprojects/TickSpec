namespace TickSpec

open System
open System.Collections.Generic
open System.Diagnostics
open System.Diagnostics.SymbolStore
open System.IO
open System.Reflection
open System.Reflection.Emit
open System.Reflection.Metadata
open System.Reflection.Metadata.Ecma335
open System.Reflection.PortableExecutable
open System.Runtime.Loader
open TickSpec.ScenarioGen

type internal FeatureGen(featureName:string, documentUrl:string) =
    // Create unique assembly name to avoid conflicts when multiple features are loaded
    let assemblyName = sprintf "Feature_%s_%s" (featureName.Replace(" ", "_")) (Guid.NewGuid().ToString("N"))
    /// Feature persisted assembly builder (for PDB support)
    let assemblyBuilder =
        PersistedAssemblyBuilder(
            AssemblyName(assemblyName),
            typeof<obj>.Assembly)
    /// Separate load context for this feature assembly
    let loadContext = AssemblyLoadContext(assemblyName, isCollectible = true)
    /// Set assembly debuggable attribute
    do  let debuggableAttribute =
            let ctor =
                let da = typeof<DebuggableAttribute>
                da.GetConstructor [|typeof<DebuggableAttribute.DebuggingModes>|]
            let arg =
                DebuggableAttribute.DebuggingModes.DisableOptimizations |||
                DebuggableAttribute.DebuggingModes.Default
            CustomAttributeBuilder(ctor, [|box arg|])
        assemblyBuilder.SetCustomAttribute debuggableAttribute
    /// Feature dynamic module
    let module_ =
        assemblyBuilder.DefineDynamicModule(featureName+".dll")
    /// Document writer for sequence points
    let doc = module_.DefineDocument(documentUrl, SymLanguageType.CSharp, SymLanguageVendor.Microsoft, SymDocumentType.Text)

    /// Mutable to track if assembly has been saved
    let mutable savedAssembly : Assembly option = None

    /// Save the assembly to memory and load it
    member private this.EnsureAssemblyLoaded() =
        match savedAssembly with
        | Some asm -> asm
        | None ->
            // Generate metadata with PDB support using out parameters
            let mutable ilStream : BlobBuilder = null
            let mutable mappedFieldData : BlobBuilder = null
            let mutable pdbBuilder : MetadataBuilder = null
            let metadataBuilder = assemblyBuilder.GenerateMetadata(&ilStream, &mappedFieldData, &pdbBuilder)

            // Get row counts for PDB builder
            let rowCounts = metadataBuilder.GetRowCounts()

            // Create portable PDB builder (no entry point for DLL)
            let portablePdbBuilder =
                PortablePdbBuilder(
                    pdbBuilder,
                    rowCounts,
                    Unchecked.defaultof<MethodDefinitionHandle>)

            // Serialize PDB to get content ID
            let pdbBlobBuilder = BlobBuilder()
            let pdbContentId = portablePdbBuilder.Serialize(pdbBlobBuilder)
            let pdbBytes = pdbBlobBuilder.ToArray()

            // Create debug directory entry
            let debugDirectoryBuilder = DebugDirectoryBuilder()
            debugDirectoryBuilder.AddCodeViewEntry(assemblyName + ".pdb", pdbContentId, portablePdbBuilder.FormatVersion)

            // Create PE with debug info
            let peHeaderBuilder = PEHeaderBuilder(imageCharacteristics = Characteristics.Dll)

            let peBuilder =
                ManagedPEBuilder(
                    peHeaderBuilder,
                    MetadataRootBuilder(metadataBuilder),
                    ilStream,
                    mappedFieldData = mappedFieldData,
                    debugDirectoryBuilder = debugDirectoryBuilder)

            // Write PE to byte array
            let peBlobBuilder = BlobBuilder()
            peBuilder.Serialize(peBlobBuilder) |> ignore
            let peBytes = peBlobBuilder.ToArray()

            // Load assembly from stream with PDB for debugging
            use peStream = new MemoryStream(peBytes)
            use pdbStream = new MemoryStream(pdbBytes)
            let asm = loadContext.LoadFromStream(peStream, pdbStream)
            savedAssembly <- Some asm
            asm

    /// Assembly of generated feature
    member this.Assembly = this.EnsureAssemblyLoaded()

    /// Gets the document writer for sequence points
    member this.Document = doc

    /// Generates scenario type from lines
    member this.GenScenario
        (events)
        (parsers:IDictionary<Type,MethodInfo>)
        (scenarioName,
         lines:(LineSource * MethodInfo * string[]) [],
         parameters:(string * string)[]) =
        generateScenario module_ doc events parsers (scenarioName,lines,parameters)
