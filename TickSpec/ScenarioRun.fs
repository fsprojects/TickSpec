module internal TickSpec.ScenarioRun

open System
open System.Collections
open System.Text.RegularExpressions
open System.Reflection
   
/// Invokes method with match values as arguments
let invoke (provider:IServiceProvider) (m:MethodInfo,args:string[],table:Table option) =   
    let buildArgs (xs:string[],m:MethodInfo) =    
        let ps = m.GetParameters()
        args |> Array.mapi (fun i x ->        
            let p = ps.[i].ParameterType
            if p.IsEnum then Enum.Parse(p,x)
            else Convert.ChangeType(x,p)           
        )             
    let instance =
        if m.IsStatic then null                
        else provider.GetService m.DeclaringType
    let args = buildArgs (args,m)
    let addTable = function
        | Some x -> Array.append args [|box x|]
        | None -> args 
    m.Invoke(instance, addTable table) |> ignore  

/// Execute feature lines
let execute (provider:IServiceProvider) (scenario,lines) =    
    lines |> Seq.iter (fun (_,n,line,m,args,table) ->
        System.Diagnostics.Debug.WriteLine line
        (m,args,table) |> invoke provider
    )
    


