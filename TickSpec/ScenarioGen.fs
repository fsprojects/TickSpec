module internal TickSpec.ScenarioGen

open System
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit
open Microsoft.FSharp.Reflection

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
        FieldAttributes.Private ||| FieldAttributes.InitOnly)

/// Defines Constructor
let defineCons
        (scenarioBuilder:TypeBuilder)
        (providerField:FieldBuilder)
        (parameters:(string * string)[]) =
    let cons =
        scenarioBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [||])
    let gen = cons.GetILGenerator()
    // Call base constructor
    gen.Emit(OpCodes.Ldarg_0)
    gen.Emit(OpCodes.Call, typeof<obj>.GetConstructor(Type.EmptyTypes))

    // Emit provider field
    gen.Emit(OpCodes.Ldarg_0)
    let ctor = typeof<ServiceProvider>.GetConstructor([||])
    gen.Emit(OpCodes.Newobj,ctor)
    gen.Emit(OpCodes.Stfld,providerField)

    // Emit example parameters
    parameters |> Seq.iter (fun (name,value) ->
        let field =
            scenarioBuilder.DefineField(
                name,
                typeof<string>,
                FieldAttributes.Private ||| FieldAttributes.InitOnly)
        gen.Emit(OpCodes.Ldarg_0)
        gen.Emit(OpCodes.Ldstr,value)
        gen.Emit(OpCodes.Stfld,field)
    )
    // Emit return
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

/// Emit instance of specified type (obtained from service provider)
let emitInstance (gen:ILGenerator) (providerField:FieldBuilder) (t:Type) =
    gen.Emit(OpCodes.Ldarg_0)
    gen.Emit(OpCodes.Ldfld,providerField)
    gen.Emit(OpCodes.Ldtoken,t)
    let getType =
        typeof<Type>.GetMethod("GetTypeFromHandle",
            [|typeof<RuntimeTypeHandle>|])
    gen.EmitCall(OpCodes.Call,getType,null)
    let getService =
        typeof<System.IServiceProvider>
            .GetMethod("GetService",[|typeof<Type>|])
    gen.Emit(OpCodes.Callvirt,getService)
    gen.Emit(OpCodes.Unbox_Any,t)

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
    let invariant =
        typeof<System.Globalization.CultureInfo>.GetMethod("get_InvariantCulture")
    gen.EmitCall(OpCodes.Call,invariant,null)
    gen.Emit(OpCodes.Unbox_Any, typeof<IFormatProvider>)
    let changeType =
        typeof<Convert>.GetMethod("ChangeType",
            [|typeof<obj>;typeof<Type>;typeof<IFormatProvider>|])
    gen.EmitCall(OpCodes.Call,changeType,null)
    // Emit cast to parameter type
    gen.Emit(OpCodes.Unbox_Any, t)

/// Emits throw
let emitThrow (gen:ILGenerator) (exnType:Type) (message:string) =
    gen.Emit(OpCodes.Ldstr, message)
    let ci = exnType.GetConstructor([|typeof<string>|])
    gen.Emit(OpCodes.Newobj, ci)
    gen.Emit(OpCodes.Throw)

/// Emits union case
let emitUnionCase (gen:ILGenerator) (paramType:Type) (arg:string) =
    let emitGetCaseAt (unionIndex:int) =
        let mi =
            typeof<FSharpType>.GetMethod("GetUnionCases",
                [|typeof<Type>;typeof<BindingFlags option>|])
        emitType gen paramType
        gen.Emit(OpCodes.Ldnull)
        gen.EmitCall(OpCodes.Call,mi,null)
        let mi =
            typeof<FSharpValue>.GetMethod("MakeUnion",
                [|typeof<UnionCaseInfo>;typeof<obj[]>;typeof<BindingFlags option>|])
        gen.Emit(OpCodes.Ldc_I4,unionIndex)
        gen.Emit(OpCodes.Ldelem, typeof<UnionCaseInfo>)
        gen.Emit(OpCodes.Ldc_I4,0)
        gen.Emit(OpCodes.Newarr,typeof<obj>)
        gen.Emit(OpCodes.Ldnull)
        gen.EmitCall(OpCodes.Call,mi,null)
    let cases = FSharpType.GetUnionCases paramType
    let equal a b = String.Compare(a ,b,StringComparison.InvariantCultureIgnoreCase) = 0
    match cases |> Array.tryFindIndex (fun case -> equal arg case.Name) with
    | Some index ->
        if cases.[index].GetFields().Length > 0 then
            sprintf "Requested value '%s' has no default constructor" arg
            |> emitThrow gen typeof<System.ArgumentException>
        else emitGetCaseAt index
    | None ->
        sprintf "Requested value '%s' was not found." arg
        |> emitThrow gen typeof<System.ArgumentException>

/// Emits value
let rec emitValue
        (gen:ILGenerator)
        (providerField:FieldBuilder)
        (parsers:IDictionary<Type,MethodInfo>)
        (paramType:Type)
        (arg:string) =
    let hasParser, parser = parsers.TryGetValue(paramType)
    if hasParser then
        gen.Emit(OpCodes.Ldstr,arg)
        if not parser.IsStatic then
            emitInstance gen providerField parser.DeclaringType
        gen.EmitCall(OpCodes.Call,parser,null)
    elif paramType = typeof<string> then
        gen.Emit(OpCodes.Ldstr,arg) // Emit string argument
    elif paramType.IsEnum then
        // Emit: System.Enum.Parse(typeof<specified argument>,arg)
        emitType gen paramType
        gen.Emit(OpCodes.Ldstr,arg)
        let mi =
            typeof<Enum>.GetMethod("Parse",
                [|typeof<Type>;typeof<string>|])
        gen.EmitCall(OpCodes.Call,mi,null)
        // Emit cast to parameter type
        gen.Emit(OpCodes.Unbox_Any,paramType)
    elif FSharpType.IsTuple paramType then
        let emitValue = emitValue gen providerField parsers
        emitTuple gen emitValue paramType arg
    elif FSharpType.IsUnion paramType then
        emitUnionCase gen paramType arg
    else
        emitConvert gen paramType arg
/// Emits tuple
and emitTuple (gen:ILGenerator) (emitValue) (paramType:Type) (arg:string) =
    // Tuple elements
    let ts = FSharpType.GetTupleElements(paramType)
    let args =
        if String.IsNullOrEmpty(arg.Trim()) then [||]
        else arg.Split [|','|] |> Array.map (fun x -> x.Trim())
    // Define local variable for temporary array
    let localArray = gen.DeclareLocal(typeof<obj[]>).LocalIndex
    // New array
    gen.Emit(OpCodes.Ldc_I4, ts.Length)
    gen.Emit(OpCodes.Newarr,typeof<obj>)
    gen.Emit(OpCodes.Stloc, localArray)
    // Set array values
    Array.zip ts args
    |> Array.iteri (fun i (t,arg) ->
        gen.Emit(OpCodes.Ldloc, localArray)
        gen.Emit(OpCodes.Ldc_I4,i)
        emitValue t arg
        gen.Emit(OpCodes.Box, t)
        gen.Emit(OpCodes.Stelem, typeof<obj>)
    )
    // Make tuple
    let mi = typeof<FSharpValue>.GetMethod("MakeTuple")
    gen.Emit(OpCodes.Ldloc, localArray)
    emitType gen paramType
    gen.EmitCall(OpCodes.Call, mi, null)

/// Emits array
let emitArray
        (gen:ILGenerator)
        (providerField:FieldBuilder)
        (parsers:IDictionary<Type,MethodInfo>)
        (paramType:Type)
        (vs:string[]) =
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
        emitValue gen providerField parsers t x
        gen.Emit(OpCodes.Stelem,t)
    )
    gen.Emit(OpCodes.Ldloc, local)

/// Emits object array based on table using object's constructor
let emitObjectArray
        (gen:ILGenerator)
        (providerField:FieldBuilder)
        (parsers:IDictionary<Type,MethodInfo>)
        (paramType:Type)
        (table:Table)
        (ci:ConstructorInfo) =
    let t = paramType.GetElementType()
    // Define local variables
    let localArray = gen.DeclareLocal(paramType).LocalIndex
    let localItem = gen.DeclareLocal(t).LocalIndex
    // Define array
    gen.Emit(OpCodes.Ldc_I4, table.Rows.Length)
    gen.Emit(OpCodes.Newarr,t)
    gen.Emit(OpCodes.Stloc, localArray)
    // Set array values
    table.Rows |> Seq.iteri (fun y row ->
        // Construct item
        for x = 0 to table.Header.Length-1 do
            let value = row.[x]
            let t = ci.GetParameters().[x].ParameterType
            emitValue gen providerField parsers t value
        gen.Emit(OpCodes.Newobj, ci)
        gen.Emit(OpCodes.Stloc, localItem)
        // Store item
        gen.Emit(OpCodes.Ldloc, localArray)
        gen.Emit(OpCodes.Ldc_I4,y)
        gen.Emit(OpCodes.Ldloc, localItem)
        gen.Emit(OpCodes.Stelem,t)
    )
    gen.Emit(OpCodes.Ldloc, localArray)

/// Emits argument
let emitArgument
        (gen:ILGenerator)
        (providerField:FieldBuilder)
        (parsers:IDictionary<Type,MethodInfo>)
        (arg:string,param:ParameterInfo) =

    let paramType = param.ParameterType
    if paramType.IsArray then
        let vs =
            if String.IsNullOrEmpty(arg.Trim()) then [||]
            else arg.Split [|','|] |> Array.map (fun x -> x.Trim())
        emitArray gen providerField parsers paramType vs
    else
        emitValue gen providerField parsers paramType arg

/// Defines step method
let defineStepMethod
        doc
        (scenarioBuilder:TypeBuilder)
        (providerField:FieldBuilder)
        (parsers:IDictionary<Type,MethodInfo>)
        (line:LineSource,mi:MethodInfo,args:string[]) =
    /// Line number
    let n = line.Number
    /// Step method builder
    let stepMethod =
        scenarioBuilder.DefineMethod(sprintf "%d: %s" n line.Text,
            MethodAttributes.Public,
            typeof<Void>,
             [||])
    /// Step method ILGenerator
    let gen = stepMethod.GetILGenerator()
    // Set marker in source document
    gen.MarkSequencePoint(doc,n,1,n,line.Text.Length+1)
    // Handle generic methods
    let mi =
        if mi.ContainsGenericParameters then
            let ps =
                mi.GetGenericArguments()
                |> Array.map (fun p ->
                    if p.IsGenericParameter then typeof<string>
                    else p
                )
            mi.MakeGenericMethod(ps)
        else
            mi
    // Emit arguments
    let ps = mi.GetParameters()
    let zipped = Seq.zip args ps

    let temp = gen.DeclareLocal(typeof<Exception>).LocalIndex
    let locals = [|for (_,p) in zipped -> gen.DeclareLocal(p.ParameterType).LocalIndex|]
    zipped
    |> Seq.iteri (fun i (arg,p) ->
        // try {
        let block = gen.BeginExceptionBlock()
        emitArgument gen providerField parsers (arg,p)
        gen.Emit(OpCodes.Stloc, locals.[i])
        // }
        gen.Emit(OpCodes.Leave_S, block)
        // catch(Exception ex) {
        gen.BeginCatchBlock(typeof<Exception>)
        // throw ArgumentException(message, name, ex);
        gen.Emit(OpCodes.Stloc, temp)
        let message =
            sprintf "Failed to convert argument '%s' of target method '%s' from '%s' to type '%s'"
                p.Name mi.Name arg p.ParameterType.Name
        gen.Emit(OpCodes.Ldstr, message)
        gen.Emit(OpCodes.Ldstr, p.Name)
        gen.Emit(OpCodes.Ldloc, temp)
        let ci =
            typeof<ArgumentException>
                .GetConstructor([|typeof<string>;typeof<string>;typeof<Exception>|])
        gen.Emit(OpCodes.Newobj, ci)
        gen.Emit(OpCodes.Throw)
        // }
        gen.EndExceptionBlock()
    )
    // For instance methods get instance value from service provider
    if not mi.IsStatic then
        emitInstance gen providerField mi.DeclaringType
    locals |> Seq.iter (fun local ->
        gen.Emit(OpCodes.Ldloc, local)
    )

    // Emit bullets argument
    line.Bullets |> Option.iter (fun x ->
        let t = (ps.[ps.Length-1].ParameterType)
        emitArray gen providerField parsers t x
    )
    // Emit table argument
    line.Table |> Option.iter (fun table ->
        let p = ps.[ps.Length-1]
        let t = p.ParameterType
        if t = typeof<Table> then emitTable gen table
        elif t.IsArray then
            let found =
                t.GetElementType().GetConstructors()
                |> Seq.tryFind (fun c -> c.GetParameters().Length = table.Header.Length)
            match found with
            | Some ci -> emitObjectArray gen providerField parsers t table ci
            | None ->
                let ci = typeof<ArgumentException>.GetConstructor([|typeof<string>;typeof<string>|])
                gen.Emit(OpCodes.Ldstr, sprintf "No matching constructor found on type: %s" (t.GetElementType().Name))
                gen.Emit(OpCodes.Ldstr, p.Name)
                gen.Emit(OpCodes.Newobj, ci)
                gen.Emit(OpCodes.Throw)
        else
            let ci = typeof<ArgumentException>.GetConstructor([|typeof<string>;typeof<string>|])
            gen.Emit(OpCodes.Ldstr, "Expecting table or array argument")
            gen.Emit(OpCodes.Ldstr, p.Name)
            gen.Emit(OpCodes.Newobj, ci)
            gen.Emit(OpCodes.Throw)
    )
    // Emit doc argument
    line.Doc |> Option.iter (fun doc -> gen.Emit(OpCodes.Ldstr, doc))
    // Emit method invoke
    if mi.IsStatic then
        gen.EmitCall(OpCodes.Call, mi, null)
    else
        gen.Emit(OpCodes.Callvirt, mi)
    // Emit return
    gen.Emit(OpCodes.Ret)
    // Return step method
    stepMethod

/// Defines Run method
let defineRunMethod
    (scenarioBuilder:TypeBuilder)
    (providerField:FieldBuilder)
    (beforeScenarioEvents:MethodInfo seq,
     afterScenarioEvents:MethodInfo seq,
     beforeStepEvents:MethodInfo seq,
     afterStepEvents:MethodInfo seq)
    (stepMethods:seq<MethodBuilder>) =
    /// Run method to execute all scenario steps
    let runMethod =
        scenarioBuilder.DefineMethod("Run",
            MethodAttributes.Public,
            typeof<Void>,
            [||])
    /// Run method ILGenerator
    let gen = runMethod.GetILGenerator()

    // Emit event methods
    let emitEvents (ms:MethodInfo seq) =
        ms |> Seq.iter (fun mi ->
            if mi.IsStatic then
                gen.EmitCall(OpCodes.Call, mi, null)
            else
                emitInstance gen providerField mi.DeclaringType
                gen.EmitCall(OpCodes.Callvirt, mi, null)
        )

    beforeScenarioEvents |> emitEvents
    // Outer exception block ensuring that the ServiceProvider will be disposed
    let exitOuter = gen.BeginExceptionBlock()
    // Inner exception block ensuring that the after scenario events will be executed
    let exit = gen.BeginExceptionBlock()
    // Execute steps
    stepMethods |> Seq.iter (fun stepMethod ->
        let exit = gen.BeginExceptionBlock()
        beforeStepEvents |> emitEvents
        gen.Emit(OpCodes.Ldarg_0)
        gen.Emit(OpCodes.Callvirt,stepMethod)
        gen.Emit(OpCodes.Leave_S, exit)
        gen.BeginFinallyBlock()
        afterStepEvents |> emitEvents
        gen.EndExceptionBlock()
    )
    gen.Emit(OpCodes.Leave_S, exit)
    gen.BeginFinallyBlock()
    // Execute after scenario events
    afterScenarioEvents |> emitEvents
    gen.EndExceptionBlock()
    gen.Emit(OpCodes.Leave_S, exitOuter)
    gen.BeginFinallyBlock()
    // Dispose the ServiceProvider
    gen.Emit(OpCodes.Ldarg_0)
    gen.Emit(OpCodes.Ldfld, providerField)
    gen.Emit(OpCodes.Isinst, typeof<IDisposable>)
    let labelNoDispose = gen.DefineLabel();
    // ... iff it is actually IDisposable
    gen.Emit(OpCodes.Brfalse_S, labelNoDispose)
    gen.Emit(OpCodes.Ldarg_0)
    gen.Emit(OpCodes.Ldfld, providerField)
    gen.Emit(OpCodes.Callvirt, typeof<IDisposable>.GetMethod("Dispose"))
    gen.MarkLabel(labelNoDispose)
    gen.EndExceptionBlock()
    // Emit return
    gen.Emit(OpCodes.Ret)

/// Generates Type for specified Scenario
let generateScenario
        (module_:ModuleBuilder)
        doc
        events
        (parsers:IDictionary<Type,MethodInfo>)
        (scenarioName,lines:(LineSource * MethodInfo * string[]) [],
         parameters:(string * string)[]) =

    let scenarioBuilder =
        defineScenarioType module_ scenarioName

    let providerField = defineProviderField scenarioBuilder

    defineCons scenarioBuilder providerField parameters

    /// Scenario step methods
    let stepMethods =
        lines
        |> Array.map (defineStepMethod doc scenarioBuilder providerField parsers)

    defineRunMethod scenarioBuilder providerField events stepMethods
    /// Return scenario
    scenarioBuilder.CreateType()