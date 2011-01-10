namespace TickSpec

open System
open System.Collections.Generic
open System.Diagnostics
open System.Reflection
open System.Reflection.Emit
open System.Threading
open TickSpec.ScenarioGen

type internal FeatureGen(featureName:string,documentUrl:string) =
    let assemblyName = "Feature"
    /// Feature dynamic assembly
    let assemblyBuilder =
        AppDomain.CurrentDomain
            .DefineDynamicAssembly(
                AssemblyName(assemblyName),
                AssemblyBuilderAccess.Run)
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
        assemblyBuilder.DefineDynamicModule
            (featureName+".dll", true)
    /// Feature source document
    let doc = module_.DefineDocument(documentUrl, Guid.Empty, Guid.Empty, Guid.Empty) 
    /// Assembly of generated feature
    member this.Assembly = assemblyBuilder :> Assembly
    /// Generates scenario type from lines
    member this.GenScenario       
        (parsers:IDictionary<Type,MethodInfo>)
        (scenarioName,
         lines:(LineSource * MethodInfo * string[]) [], 
         parameters:(string * string)[]) =
        generateScenario module_ doc parsers (scenarioName,lines,parameters)
