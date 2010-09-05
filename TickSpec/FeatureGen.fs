namespace TickSpec

open System
open System.Diagnostics
open System.Reflection
open System.Reflection.Emit
open System.Threading
open TickSpec.ScenarioGen

type internal FeatureGen(featureName:string,documentUrl:string) =
    let assemblyName = "Feature"
    /// Feature dynamic assembly
    let assemblyBuilder =      
        Thread.GetDomain()
            .DefineDynamicAssembly(
                AssemblyName(assemblyName), 
                AssemblyBuilderAccess.RunAndSave)
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
    /// Generates scenario type from lines
    member this.GenScenario 
        (provider:IServiceProvider)
        (scenarioName,lines:(string * int * string * MethodInfo * string[]) []) =
        let scenario = GenScenario module_ doc (scenarioName,lines)
        //assemblyBuilder.Save(assemblyName+".dll")
        let cons = scenario.GetConstructor([|typeof<IServiceProvider>|])                     
        let instance = cons.Invoke([|provider|])
        instance
    