module internal TickSpec.ScenarioRun

open System
open System.Collections
open System.Text.RegularExpressions
open System.Reflection
   
let toArray (t:Type) (s:string) =    
    let vs =              
        if String.IsNullOrEmpty(s.Trim()) then [||]                                 
        else s.Split [|','|]               
        |> Array.map (fun x -> x.Trim())
        |> Array.map (fun x -> Convert.ChangeType(x,t))
    let ar = Array.CreateInstance(t,vs.Length)
    for i = 0 to ar.Length-1 do ar.SetValue(vs.[i],i)
    ar
   
/// Invokes method with match values as arguments
let invoke (provider:IServiceProvider) (m:MethodInfo,args:string[],table:Table option) =   
    let buildArgs (xs:string[],m:MethodInfo) =    
        let ps = m.GetParameters()
        args |> Array.mapi (fun i s ->        
            let p = ps.[i].ParameterType
            if p.IsEnum then Enum.Parse(p,s)
            elif p.IsArray then                     
                toArray (p.GetElementType()) s |> box
            else Convert.ChangeType(s,p)           
        )             
    let instance =
        if m.IsStatic then null                
        else provider.GetService m.DeclaringType
    let args = buildArgs (args,m)
    let addTable = function
        | Some x -> Array.append args [|box x|]
        | None -> args 
    m.Invoke(instance, addTable table) |> ignore  

/// Generate execution function
let generate (provider:IServiceProvider) (scenario,lines) =    
    fun () ->
        lines |> Seq.iter (fun (_,n,line,m,args,table) ->
            System.Diagnostics.Debug.WriteLine line
            (m,args,table) |> invoke provider)    
    


