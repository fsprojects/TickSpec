namespace TickSpec

open System

/// Action type
type Action = delegate of unit -> unit

/// Encapsulates Gherkin feature
type Feature = { 
    Name : string; 
    Source : string;
    Assembly : System.Reflection.Assembly;
    Scenarios : Scenario seq
    }
/// Executable scenario type
and Scenario = { 
    Name:string; 
    Description:string;
    Action:Action;
    Parameters:(string * string)[]
    Tags:string[]
    }
    with 
    override this.ToString() = 
        if this.Parameters.Length=0 then this.Name
        else            
            let ps =
                this.Parameters  
                |> Array.map (fun (k,v) -> sprintf "%s=%s" k v)
                |> String.concat ","
            sprintf "%s{%s}" this.Name ps

