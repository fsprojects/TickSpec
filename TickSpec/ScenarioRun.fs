module internal TickSpec.ScenarioRun

open System
open System.Collections
open System.Text.RegularExpressions
open System.Reflection
   
/// Splits CSV
let split (s:string) =
    if String.IsNullOrEmpty(s.Trim()) then [||]
    else s.Split [|','|]
         |> Array.map (fun x -> x.Trim())

/// Converts string array to array of specified type
let toArray (t:Type) (xs:string[]) =
    let vs = xs |> Array.map (fun x -> Convert.ChangeType(x,t))
    let ar = Array.CreateInstance(t,vs.Length)
    for i = 0 to ar.Length-1 do ar.SetValue(vs.[i],i)
    ar
   
/// Invokes method with match values as arguments
let invoke 
        (provider:IServiceProvider) 
        (m:MethodInfo,args:string[],
         bullets:string[] option,table:Table option) =
    let buildArgs (xs:string[],m:MethodInfo) =
        let ps = m.GetParameters()
        args |> Array.mapi (fun i s ->
            let p = ps.[i].ParameterType
            if p.IsEnum then Enum.Parse(p,s)
            elif p.IsArray then
                toArray (p.GetElementType()) (split s) |> box
            else Convert.ChangeType(s,p)
        )
    let instance =
        if m.IsStatic then null
        else provider.GetService m.DeclaringType
    let args = buildArgs (args,m)
    let tail =
        match bullets,table with
        | Some xs,None -> [|box (toArray (typeof<string>) xs)|]
        | None,Some x -> [|box x|]
        | _,_ -> [||]
    m.Invoke(instance, Array.append args tail) |> ignore

/// Generate execution function
let generate (provider:IServiceProvider) (scenario,lines) =
    fun () ->
        lines |> Seq.iter (fun (_,n,line,m,args,bullets,table) ->
            System.Diagnostics.Debug.WriteLine line
            (m,args,bullets,table) |> invoke provider)
