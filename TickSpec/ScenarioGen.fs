module internal TickSpec.ScenarioGen

open System
open System.Reflection
open System.Reflection.Emit
        
/// Creates type        
let CreateScenarioType 
        (module_:ModuleBuilder) 
        (scenarioName) =    
    module_.DefineType(
        scenarioName, 
        TypeAttributes.Public ||| TypeAttributes.Class)        
        
/// Create _provider field
let CreateProviderField
        (scenarioBuilder:TypeBuilder) =  
    scenarioBuilder.DefineField(
        "_provider",
        typeof<IServiceProvider>,
        FieldAttributes.Private)            
        
/// Creates Constructor
let CreateCons 
        (scenarioBuilder:TypeBuilder)
        (providerField:FieldBuilder) =
    let cons = 
        scenarioBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [|typeof<System.IServiceProvider>|])
    let gen = cons.GetILGenerator() 
    gen.Emit(OpCodes.Ldarg_0)
    gen.Emit(OpCodes.Ldarg_1)
    gen.Emit(OpCodes.Stfld,providerField)   
    gen.Emit(OpCodes.Ret)
        
/// Pushes parameter
let PushParam 
        (gen:ILGenerator) 
        (arg:string,param:ParameterInfo) =
    // Emit string argument push
    gen.Emit(OpCodes.Ldstr,arg)
    let paramType = param.ParameterType
    if paramType <> typeof<string> then                            
        // Emit: System.Convert.ChangeType(arg,typeof<specified parameter>)
        gen.Emit(OpCodes.Ldtoken,paramType)   
        let mi = 
            typeof<Type>.GetMethod("GetTypeFromHandle", 
                [|typeof<RuntimeTypeHandle>|])
        gen.EmitCall(OpCodes.Call,mi,null)         
        let mi = 
            typeof<Convert>.GetMethod("ChangeType", 
                [|typeof<obj>;typeof<Type>|])        
        gen.EmitCall(OpCodes.Call,mi,null)         
        // Emit cast to parameter type
        gen.Emit(OpCodes.Unbox_Any,paramType)        
        
/// Creates step method        
let CreateStepMethod
        doc
        (scenarioBuilder:TypeBuilder)
        (providerField:FieldBuilder)
        (_,n:int,line:string,mi:MethodInfo,args:string[]) =            
    /// Ste method builder
    let stepMethod = 
        scenarioBuilder.DefineMethod(sprintf "%d: %s" n line,
            MethodAttributes.Public, 
            typeof<Void>,                 
            [||])                
    /// Step method ILGenerator        
    let gen = stepMethod.GetILGenerator()
    // Set marker in source document
    gen.MarkSequencePoint(doc,n,1,n,line.Length+1)           
    // For instance methods get instance value from service provider 
    if not mi.IsStatic then            
        gen.Emit(OpCodes.Ldarg_0)
        gen.Emit(OpCodes.Ldfld,providerField)
        gen.Emit(OpCodes.Ldtoken,mi.DeclaringType)
        let getType = 
            typeof<Type>.GetMethod("GetTypeFromHandle", 
                [|typeof<RuntimeTypeHandle>|])
        gen.EmitCall(OpCodes.Call,getType,null) 
        let getService =
            typeof<System.IServiceProvider>
                .GetMethod("GetService",[|typeof<Type>|])
        gen.EmitCall(OpCodes.Callvirt,getService,null)
        gen.Emit(OpCodes.Unbox_Any,mi.DeclaringType)                               
    // Emit parameters
    Seq.zip args (mi.GetParameters()) 
    |> Seq.iter (PushParam gen)
    // Emit method invoke
    if mi.IsStatic then 
        gen.EmitCall(OpCodes.Call, mi, null)                
    else 
        gen.EmitCall(OpCodes.Callvirt, mi, null)    
    // Emit return          
    gen.Emit(OpCodes.Ret);
    // Return step method                           
    stepMethod
    
/// Creates Run method
let CreateRunMethod 
    (scenarioBuilder:TypeBuilder)
    (stepMethods:seq<MethodBuilder>) =
    /// Run method to execute all scenario steps
    let runMethod = 
        scenarioBuilder.DefineMethod("Run",             
            MethodAttributes.Public, 
            typeof<Void>, 
            [||])    
    /// Run method ILGenerator
    let gen = runMethod.GetILGenerator()    
    // Execute steps
    stepMethods |> Seq.iter (fun stepMethod ->
        gen.Emit(OpCodes.Ldarg_0)
        gen.EmitCall(OpCodes.Callvirt,stepMethod,null)
    )    
    // Emit return
    gen.Emit(OpCodes.Ret)    
                
/// Generates Type for specified Scenario
let GenScenario 
        (module_:ModuleBuilder) 
        doc
        (scenarioName,lines:(string * int * string * MethodInfo * string[]) []) =
    
    let scenarioBuilder = 
        CreateScenarioType module_ scenarioName
    
    let providerField = CreateProviderField scenarioBuilder
        
    CreateCons scenarioBuilder providerField
        
    /// Scenario step methods
    let stepMethods = 
        lines 
        |> Array.map (CreateStepMethod doc scenarioBuilder providerField)              
        
    CreateRunMethod scenarioBuilder stepMethods
    
    /// Return scenario
    scenarioBuilder.CreateType()
   