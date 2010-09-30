module internal TickSpec.ScenarioGen

open System
open System.Reflection
open System.Reflection.Emit
        
/// Defines scenario type
let defineScenarioType 
        (module_:ModuleBuilder) 
        (scenarioName) =
    module_.DefineType(
        scenarioName,
        TypeAttributes.Public ||| TypeAttributes.Class)
        
/// Defines _provider field
let defineProviderField
        (scenarioBuilder:TypeBuilder) =
    scenarioBuilder.DefineField(
        "_provider",
        typeof<IServiceProvider>,
        FieldAttributes.Private)
        
/// Defines Constructor
let defineCons 
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
        
/// Emits table argument    
let emitTable
        (gen:ILGenerator)
        (table:Table) =
        
    let local0 = gen.DeclareLocal(typeof<string[]>).LocalIndex
    let local1 = gen.DeclareLocal(typeof<string[][]>).LocalIndex

    // Define header
    gen.Emit(OpCodes.Ldc_I4, table.Header.Length)
    gen.Emit(OpCodes.Newarr,typeof<string>)
    gen.Emit(OpCodes.Stloc,local0)
    // Fill header
    table.Header |> Seq.iteri (fun i s ->
        gen.Emit(OpCodes.Ldloc,local0)
        gen.Emit(OpCodes.Ldc_I4, i)
        gen.Emit(OpCodes.Ldstr,s)
        gen.Emit(OpCodes.Stelem_Ref)
    )
    gen.Emit(OpCodes.Ldloc,local0)
    // Define rows
    gen.Emit(OpCodes.Ldc_I4,table.Rows.Length)
    gen.Emit(OpCodes.Newarr,typeof<string[]>)
    gen.Emit(OpCodes.Stloc,local1)
    // Fill rows
    table.Rows |> Seq.iteri (fun y row ->
        // Define row
        gen.Emit(OpCodes.Ldloc,local1)
        gen.Emit(OpCodes.Ldc_I4,y)
        gen.Emit(OpCodes.Ldc_I4,row.Length)
        gen.Emit(OpCodes.Newarr,typeof<string>)
        gen.Emit(OpCodes.Stloc,local0)
        // Fill columns
        row |> Seq.iteri (fun x col ->
            gen.Emit(OpCodes.Ldloc,local0)
            gen.Emit(OpCodes.Ldc_I4,x)
            gen.Emit(OpCodes.Ldstr,col)
            gen.Emit(OpCodes.Stelem_Ref)
        )
        gen.Emit(OpCodes.Ldloc,local0)
        gen.Emit(OpCodes.Stelem_Ref)
    )
    // Instantiate table
    gen.Emit(OpCodes.Ldloc,local1)
    let ci = 
        typeof<Table>.GetConstructor(
            [|typeof<string[]>;typeof<string[][]>|])
    gen.Emit(OpCodes.Newobj,ci)
                        
/// Emits type argument
let emitType (gen:ILGenerator) (t:Type) =
    gen.Emit(OpCodes.Ldtoken,t)
    let mi =
        typeof<Type>.GetMethod("GetTypeFromHandle",
            [|typeof<RuntimeTypeHandle>|])
    gen.EmitCall(OpCodes.Call,mi,null)
                        
/// Emits conversion function
let emitConvert (gen:ILGenerator) (t:Type) (x:string) =
    // Emit: System.Convert.ChangeType(arg,typeof<specified parameter>)
    gen.Emit(OpCodes.Ldstr, x)
    emitType gen t
    let mi =
        typeof<Convert>.GetMethod("ChangeType",
            [|typeof<obj>;typeof<Type>|])
    gen.EmitCall(OpCodes.Call,mi,null)
    // Emit cast to parameter type
    gen.Emit(OpCodes.Unbox_Any, t)
    
/// Emits value    
let emitValue (gen:ILGenerator) (t:Type) (x:string) =
    if  t = typeof<string> then
        gen.Emit(OpCodes.Ldstr,x) // Emit string argument
    else
        emitConvert gen t x
        
/// Emits array
let emitArray (gen:ILGenerator) (paramType:Type) (vs:string[]) =
    let t = paramType.GetElementType()
    // Define local variable
    let local = gen.DeclareLocal(paramType).LocalIndex
    // Define array
    gen.Emit(OpCodes.Ldc_I4, vs.Length)
    gen.Emit(OpCodes.Newarr,t)
    gen.Emit(OpCodes.Stloc, local)
    // Set array values
    vs |> Seq.iteri (fun i x ->
        gen.Emit(OpCodes.Ldloc, local)
        gen.Emit(OpCodes.Ldc_I4,i)
        emitValue gen t x
        gen.Emit(OpCodes.Stelem,t)
    )
    gen.Emit(OpCodes.Ldloc, local)
        
/// Emits argument
let emitArgument
        (gen:ILGenerator) 
        (arg:string,param:ParameterInfo) =
        
    let paramType = param.ParameterType
    if paramType.IsEnum then
        // Emit: System.Enum.Parse(typeof<specified argument>,arg)
        emitType gen paramType
        gen.Emit(OpCodes.Ldstr,arg)
        let mi = 
            typeof<Enum>.GetMethod("Parse", 
                [|typeof<Type>;typeof<string>|])
        gen.EmitCall(OpCodes.Call,mi,null)
        // Emit cast to parameter type
        gen.Emit(OpCodes.Unbox_Any,paramType)
    elif paramType.IsArray then
        let vs =
            if String.IsNullOrEmpty(arg.Trim()) then [||]
            else arg.Split [|','|] |> Array.map (fun x -> x.Trim())
        emitArray gen paramType vs
    else
        emitValue gen paramType arg
        
/// Defines step method
let defineStepMethod
        doc
        (scenarioBuilder:TypeBuilder)
        (providerField:FieldBuilder)
        (_,n:int,line:string,mi:MethodInfo,args:string[],
         bullets:string[] option,table:Table option) =
    /// Step method builder
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
    // Emit arguments
    let ps = mi.GetParameters()
    Seq.zip args ps
    |> Seq.iter (emitArgument gen)
    // Emit bullets argument
    bullets |> Option.iter (fun x ->
        let t = (ps.[ps.Length-1].ParameterType)
        emitArray gen t x
    )
    // Emit table argument
    table |> Option.iter (emitTable gen)
    // Emit method invoke
    if mi.IsStatic then
        gen.EmitCall(OpCodes.Call, mi, null)
    else
        gen.EmitCall(OpCodes.Callvirt, mi, null)
    // Emit return
    gen.Emit(OpCodes.Ret);
    // Return step method
    stepMethod
    
/// Defines Run method
let defineRunMethod
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
let generateScenario 
        (module_:ModuleBuilder)
        doc
        (scenarioName,lines:(string * int * string * MethodInfo * string[]
                             * string[] option * Table option) []) =
    
    let scenarioBuilder =
        defineScenarioType module_ scenarioName
    
    let providerField = defineProviderField scenarioBuilder
        
    defineCons scenarioBuilder providerField
        
    /// Scenario step methods
    let stepMethods =
        lines 
        |> Array.map (defineStepMethod doc scenarioBuilder providerField)
        
    defineRunMethod scenarioBuilder stepMethods
    
    /// Return scenario
    scenarioBuilder.CreateType()