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
        
/// Pushes table parameter    
let PushTable
        (gen:ILGenerator)        
        (table:Table) =
        
    gen.DeclareLocal(typeof<string[]>) |> ignore
    gen.DeclareLocal(typeof<string[][]>) |> ignore

    // Define header           
    gen.Emit(OpCodes.Ldc_I4, table.Header.Length)
    gen.Emit(OpCodes.Newarr,typeof<string>)
    gen.Emit(OpCodes.Stloc_0)
    // Fill header
    table.Header |> Seq.iteri (fun i s ->        
        gen.Emit(OpCodes.Ldloc_0)
        gen.Emit(OpCodes.Ldc_I4, i)
        gen.Emit(OpCodes.Ldstr,s)
        gen.Emit(OpCodes.Stelem_Ref)
    )
    gen.Emit(OpCodes.Ldloc,0)       
    // Define rows
    gen.Emit(OpCodes.Ldc_I4,table.Rows.Length)
    gen.Emit(OpCodes.Newarr,typeof<string[]>)
    gen.Emit(OpCodes.Stloc,1)     
    // Fill rows
    table.Rows |> Seq.iteri (fun y row ->
        // Define row
        gen.Emit(OpCodes.Ldloc,1)
        gen.Emit(OpCodes.Ldc_I4,y)
        gen.Emit(OpCodes.Ldc_I4,row.Length)
        gen.Emit(OpCodes.Newarr,typeof<string>)
        gen.Emit(OpCodes.Stloc,0)
        // Fill columns
        row |> Seq.iteri (fun x col ->
            gen.Emit(OpCodes.Ldloc,0)
            gen.Emit(OpCodes.Ldc_I4,x)
            gen.Emit(OpCodes.Ldstr,col)
            gen.Emit(OpCodes.Stelem_Ref)           
        )
        gen.Emit(OpCodes.Ldloc,0)
        gen.Emit(OpCodes.Stelem_Ref)
    )
    // Instantiate table
    gen.Emit(OpCodes.Ldloc,1)    
    let ci = typeof<Table>.GetConstructor([|typeof<string[]>;typeof<string[][]>|])
    gen.Emit(OpCodes.Newobj,ci)     
          
/// Pushes parameter
let PushParam 
        (gen:ILGenerator) 
        (arg:string,param:ParameterInfo) =
           
    let paramType = param.ParameterType        
    
    let emitParamType () = 
        gen.Emit(OpCodes.Ldtoken,paramType)   
        let mi = 
            typeof<Type>.GetMethod("GetTypeFromHandle", 
                [|typeof<RuntimeTypeHandle>|])
        gen.EmitCall(OpCodes.Call,mi,null)
        
    if  paramType = typeof<string> then
        // Emit string argument
        gen.Emit(OpCodes.Ldstr,arg)
    elif paramType.IsEnum then
        // Emit: System.Enum.Parse(typeof<specified argument>,arg)
        emitParamType ()
        gen.Emit(OpCodes.Ldstr,arg)
        let mi = 
            typeof<Enum>.GetMethod("Parse", 
                [|typeof<Type>;typeof<string>|])        
        gen.EmitCall(OpCodes.Call,mi,null)         
        // Emit cast to parameter type
        gen.Emit(OpCodes.Unbox_Any,paramType)        
    else                               
        // Emit: System.Convert.ChangeType(arg,typeof<specified parameter>)
        gen.Emit(OpCodes.Ldstr,arg)        
        emitParamType  ()     
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
        (_,n:int,line:string,mi:MethodInfo,args:string[],table:Table option) =            
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
    // Emit table parameter
    table |> Option.iter (PushTable gen)
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
        (scenarioName,lines:(string * int * string * MethodInfo * string[] * Table option) []) =
    
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
   