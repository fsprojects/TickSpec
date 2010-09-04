module internal TickSpec.ScenarioRun

open System
open System.Collections
open System.Text.RegularExpressions
open System.Reflection
   
/// Invokes method with match values as arguments
let invoke (provider:IServiceProvider) (m:MethodInfo,args:string[]) =   
    let buildArgs (xs:string[],m:MethodInfo) =    
        Array.zip xs (m.GetParameters())
        |> Array.map (fun (x,p) ->        
            Convert.ChangeType(x,p.ParameterType)
        )         
    let instance =
        if m.IsStatic then null                
        else provider.GetService m.DeclaringType
    m.Invoke(instance, buildArgs (args,m)) |> ignore  

/// Execute feature lines
let execute (provider:IServiceProvider) (scenario,lines) =    
    lines |> Seq.iter (fun (_,n,line,m,args) ->
        System.Diagnostics.Debug.WriteLine line
        (m,args) |> invoke provider
    )
    


