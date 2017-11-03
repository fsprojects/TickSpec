module internal TickSpec.ScenarioRun

open System
open System.Collections.Generic
open System.Reflection
open Microsoft.FSharp.Reflection

/// Splits CSV
let split (s:string) =
    if String.IsNullOrEmpty(s.Trim()) then [||]
    else s.Split [|','|]
         |> Array.map (fun x -> x.Trim())

/// Gets object instance for specified method
let getInstance (provider:IInstanceProvider) (m:MethodInfo) =
    if m.IsStatic then null
    else provider.GetService m.DeclaringType

/// Invokes specified method with specified parameters
let invoke (provider:IInstanceProvider) (m:MethodInfo) ps =
    let instance = getInstance provider m
    m.Invoke(instance,ps) |> ignore

/// Converts generic methods
let toConcreteMethod (m:MethodInfo) =
    if m.ContainsGenericParameters then
        let ps =
            m.GetGenericArguments()
            |> Array.map (fun p ->
                if p.IsGenericParameter then typeof<string>
                else p
            )
        m.MakeGenericMethod(ps)
    else
        m

/// Converts string array to array of specified type
let rec toArray
        (parsers:IDictionary<Type,MethodInfo>)
        provider
        (t:Type) (xs:string[]) =
    let vs = xs |> Array.map (fun x -> convertString parsers provider t x)
    let ar = Array.CreateInstance(t,vs.Length)
    for i = 0 to ar.Length-1 do ar.SetValue(vs.[i],i)
    ar
/// Converts string (s) to parameter Type (p)
and convertString
        (parsers:IDictionary<Type,MethodInfo>)
        provider (p:Type) s =
    let hasParser, parser = parsers.TryGetValue(p)
    if hasParser then
        invoke provider parser [|s|]
        parser.Invoke(getInstance provider parser, [|s|])
    elif p.IsEnum then Enum.Parse(p,s,ignoreCase=true)
    elif p.IsArray then
        toArray parsers provider (p.GetElementType()) (split s) |> box
    elif FSharpType.IsTuple p then
        let ts = FSharpType.GetTupleElements(p)
        let ar = toArray parsers provider typeof<string> (split s) :?> string[]
        let ar = Array.zip ts ar
        let xs = [|for (t,x) in ar -> convertString parsers provider t x|]
        FSharpValue.MakeTuple(xs, p)
    elif FSharpType.IsUnion p then
        let cases = FSharpType.GetUnionCases p
        let unionCase = cases |> Seq.find (fun case -> String.Compare(s, case.Name, StringComparison.InvariantCultureIgnoreCase) = 0)
        FSharpValue.MakeUnion(unionCase,[||])
    elif p.IsGenericType && p.GetGenericTypeDefinition() = typeof<System.Nullable<_>> then
        let t = p.GetGenericArguments().[0]
        let culture = System.Globalization.CultureInfo.InvariantCulture
        Convert.ChangeType(s,t,culture)
    else
        let culture = System.Globalization.CultureInfo.InvariantCulture
        Convert.ChangeType(s,p,culture)

/// Converts a table to the specified array type
let convertTable parsers provider (t:Type) (table:Table) =
    let t = t.GetElementType()
    let ar = Array.CreateInstance(t,table.Rows.Length)
    let cons =
        t.GetConstructors()
        |> Seq.tryFind (fun c -> c.GetParameters().Length = table.Header.Length)
    match cons with
    | Some c ->
        // Try to use type's constructor
        table.Rows |> Array.iteri (fun y row ->
            let ps = c.GetParameters()
            let args =
                Array.zip ps row
                |> Array.map (fun (p,s) -> convertString parsers provider p.ParameterType s)
            let e = Activator.CreateInstance(t, args)
            ar.SetValue(e, y)
        )
    | None ->
        // Try to use type's properties
        let ps = t.GetProperties()
        table.Rows |> Array.iteri (fun y row ->
            let e = Activator.CreateInstance(t)
            for x = 0 to table.Header.Length-1 do
                let column = table.Header.[x]
                let p = ps |> Seq.find (fun p -> String.Compare(p.Name, column, StringComparison.InvariantCultureIgnoreCase)=0)
                let value = convertString parsers provider p.PropertyType row.[x]
                p.SetValue(e, value, [||])
            ar.SetValue(e, y)
        )
    ar

/// Invokes method with match values as arguments
let invokeStep
        (parsers:IDictionary<Type,MethodInfo>)
        (provider:IInstanceProvider)
        (meth:MethodInfo,args:string[],
         bullets:string[] option,table:Table option,doc:string option) =
    let meth = meth |> toConcreteMethod
    let ps = meth.GetParameters()
    let buildArgs (xs:string[]) =
        args |> Array.mapi (fun i s ->
            let p = ps.[i].ParameterType
            try convertString parsers provider p s
            with ex ->
                let name = ps.[i].Name
                let message =
                    sprintf "Failed to convert argument '%s' of target method '%s' from '%s' to type '%s'"
                        name meth.Name s p.Name
                raise <| ArgumentException(message, name, ex)
        )
    let args = buildArgs args
    let tail =
        match bullets,table,doc with
        | Some xs,None,None ->
            let p = ps.[ps.Length-1]
            let t = p.ParameterType.GetElementType()
            [|box (toArray parsers provider t xs)|]
        | None,Some table,None ->
            let p = ps.[ps.Length-1].ParameterType
            if p = typeof<Table> then [|box table|]
            elif p.IsArray then [|convertTable parsers provider p table|]
            else failwith "Expecting table argument"
        | None,None,Some doc -> [|box doc|]
        | _,_,_ -> [||]
    let args = Array.append args tail
    invoke provider meth args

/// Generate scenario execution function
let generate events parsers (scenario, lines) (serviceProviderFactory: unit -> IInstanceProvider) =
    fun () ->
        /// Type instance provider
        let provider = serviceProviderFactory()

        try
            let beforeScenarioEvents, afterScenarioEvents, beforeStepEvents, afterStepEvents = events
            /// Invokes events
            let invokeEvents events =
                events |> Seq.iter (fun (mi:MethodInfo) ->
                    invoke provider mi [||]
                )
            try
                beforeScenarioEvents |> invokeEvents
                // Iterate scenario lines
                lines |> Seq.iter (fun (line:LineSource,m,args) ->
                    try
                        beforeStepEvents |> invokeEvents
                        (m,args,line.Bullets,line.Table,line.Doc) |> invokeStep parsers provider
                    finally
                        afterStepEvents |> invokeEvents
                )
            finally
                afterScenarioEvents |> invokeEvents
        finally
            match provider with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()