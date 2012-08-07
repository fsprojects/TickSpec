module internal TickSpec.ScenarioRun

open System
open System.Collections
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Reflection
open Microsoft.FSharp.Reflection

/// Splits CSV
let split (s:string) =
    if String.IsNullOrEmpty(s.Trim()) then [||]
    else s.Split [|','|]
         |> Array.map (fun x -> x.Trim())

/// Converts string array to array of specified type
let toArray (t:Type) (xs:string[]) =
    let culture = System.Globalization.CultureInfo.InvariantCulture
    let vs = xs |> Array.map (fun x -> Convert.ChangeType(x,t,culture))
    let ar = Array.CreateInstance(t,vs.Length)
    for i = 0 to ar.Length-1 do ar.SetValue(vs.[i],i)
    ar

/// Gets object instance for specified method
let getInstance (provider:IServiceProvider) (m:MethodInfo) =
    if m.IsStatic then null
    else provider.GetService m.DeclaringType

/// Invokes specified method with specified parameters
let invoke (provider:IServiceProvider) (m:MethodInfo) ps =     
    let instance = getInstance provider m
    m.Invoke(instance,ps) |> ignore
    
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

/// Invokes method with match values as arguments
let invokeStep
        (parsers:IDictionary<Type,MethodInfo>)
        (provider:IServiceProvider) 
        (meth:MethodInfo,args:string[],
         bullets:string[] option,table:Table option) =
    let meth = meth |> toConcreteMethod
    let buildArgs (xs:string[]) =
        let ps = meth.GetParameters()
        args |> Array.mapi (fun i s ->
            let p = ps.[i].ParameterType
            let hasParser, parser = parsers.TryGetValue(p)
            if hasParser then
                invoke provider parser [|s|]
                parser.Invoke(getInstance provider parser, [|s|])
            elif p.IsEnum then Enum.Parse(p,s,ignoreCase=true)
            elif p.IsArray then
                toArray (p.GetElementType()) (split s) |> box
            elif FSharpType.IsUnion p then
                let cases = FSharpType.GetUnionCases p
                let unionCase = cases |> Seq.find (fun case -> s = case.Name)
                FSharpValue.MakeUnion(unionCase,[||])
            else
                let culture = System.Globalization.CultureInfo.InvariantCulture 
                Convert.ChangeType(s,p,culture)
        )
    let args = buildArgs (args)
    let tail =
        match bullets,table with
        | Some xs,None -> [|box (toArray (typeof<string>) xs)|]
        | None,Some x -> [|box x|]
        | _,_ -> [||]
    invoke provider meth (Array.append args tail)

/// Generate scenario execution function
let generate events parsers (scenario,lines) =
    fun () ->
        /// Type instance provider
        let provider = ServiceProvider()
        let beforeScenarioEvents, afterScenarioEvents, beforeStepEvents, afterStepEvents = events
        /// Invokes events
        let invokeEvents events = 
            events |> Seq.iter (fun (mi:MethodInfo) ->
                invoke provider mi [||]               
            )
        beforeScenarioEvents |> invokeEvents
        // Iterate scenario lines
        lines |> Seq.iter (fun (line:LineSource,m,args) ->
            beforeStepEvents |> invokeEvents
            (m,args,line.Bullets,line.Table) |> invokeStep parsers provider
            afterStepEvents |> invokeEvents
        )
        afterScenarioEvents |> invokeEvents