module internal TickSpec.ScenarioGen

open System
open System.Reflection
open System.Reflection.Emit

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
    
/// Generates Type for specifed Scenario
let GenScenario 
        (module_:ModuleBuilder) 
        doc
        (scenarioName,lines:(string * int * string * MethodInfo * string[]) []) =
    // TODO: add NUnit attribute
    let scenarioBuilder = 
        module_.DefineType(
            scenarioName, 
            TypeAttributes.Public ||| TypeAttributes.Class)
    
    /// Field _provider
    let providerField = 
        scenarioBuilder.DefineField(
            "_provider",
            typeof<IServiceProvider>,
            FieldAttributes.Private)    
    
    /// Creates Constructor
    let CreateCons () =
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
    
    CreateCons()
        
    let CreateStep (_,n:int,line:string,mi:MethodInfo,args:string[]) =        
        let stepMethod = 
            scenarioBuilder.DefineMethod(sprintf "%d: %s" n line,
                MethodAttributes.Public, 
                typeof<Void>,                 
                [||])                
        /// Step method ILGenerator        
        let gen = stepMethod.GetILGenerator()                
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
        // Set marker in source document
        gen.MarkSequencePoint(doc,n,1,n,line.Length+1)
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
        
    /// Scenario steps
    let steps = lines |> Seq.map CreateStep       
    
    /// Run method to execute all scenario steps
    let runMethod = 
        scenarioBuilder.DefineMethod("Run",             
            MethodAttributes.Public, 
            typeof<Void>, 
            [||])    
    /// Run method ILGenerator
    let gen = runMethod.GetILGenerator()    
    // Execute steps
    steps |> Seq.iter (fun stepMethod ->
        gen.Emit(OpCodes.Ldarg_0)
        gen.EmitCall(OpCodes.Callvirt,stepMethod,null)
    )    
    // Emit return
    gen.Emit(OpCodes.Ret)    
    /// Return scenario Step
    scenarioBuilder.CreateType()