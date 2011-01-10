module internal TickSpec.ScenarioRun

open System
open System.Collections
open System.Collections.Generic
open System.Text.RegularExpressions
open System.Reflection
   
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
   
/// Invokes method with match values as arguments
let invoke 
        (parsers:IDictionary<Type,MethodInfo>)
        (provider:IServiceProvider) 
        (meth:MethodInfo,args:string[],
         bullets:string[] option,table:Table option) =
    let getInstance (m:MethodInfo) =
        if m.IsStatic then null
        else provider.GetService m.DeclaringType
    let buildArgs (xs:string[]) =
        let ps = meth.GetParameters()
        args |> Array.mapi (fun i s ->
            let p = ps.[i].ParameterType
            let hasParser, parser = parsers.TryGetValue(p)
            if hasParser then               
                parser.Invoke(getInstance parser, [|s|])
            elif p.IsEnum then Enum.Parse(p,s,ignoreCase=true)
            elif p.IsArray then
                toArray (p.GetElementType()) (split s) |> box
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
    meth.Invoke(getInstance meth, Array.append args tail) |> ignore

/// Generate scenario execution function
let generate parsers (scenario,lines) =
    fun () ->
        /// Type instance provider
        let provider = ServiceProvider()
        // Iterate scenario lines
        lines |> Seq.iter (fun (line:LineSource,m,args) ->
            (m,args,line.Bullets,line.Table) |> invoke parsers provider)
