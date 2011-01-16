[<AutoOpen>]
module Interactive

module internal TextReader =
    open System.IO
    /// Reads lines from TextReader
    let ReadLines (reader:System.IO.TextReader) =       
        seq {                  
            let isEOF = ref false
            while not !isEOF do
                let line = reader.ReadLine()                
                if line <> null then yield line
                else isEOF := true
        }
    /// Read all lines to a string array
    let ReadAllLines reader = reader |> ReadLines |> Seq.toArray   

/// Converts speified text to lines
let toLines text = 
    using (new System.IO.StringReader(text)) TextReader.ReadAllLines 

/// Pending exception type
exception Pending of unit
/// Raises pending exception
let pending () = raise <| Pending ()

exception StepError of string
let stepError s = raise <| StepError s

open System.Collections.Generic

let private givens, whens, thens = 
    Dictionary<_,_>(), Dictionary<_,_>(), Dictionary<_,_>()

let private SetStep 
        (stepDefinitions:Dictionary<_,_>) 
        (pattern:string) 
        (handler:string list -> unit) = 
    stepDefinitions.[pattern] <- handler

/// Registers specified pattern's handler function
let Given s f = SetStep givens s f
/// Registers specified pattern's handler function
let When s f = SetStep whens s f
/// Registers specified pattern's handler function
let Then s f = SetStep thens s f

open System
open System.Text.RegularExpressions
open TickSpec

/// Executes specified feature lines writing output to console
let Execute (lines:string[]) =  
    /// Choose all steps matching input
    let choose (steps:Dictionary<_,_>) input =
        steps 
        |> Seq.map (fun pair -> pair.Key,pair.Value)
        |> Seq.choose (fun (pattern,f) ->
            let r = Regex.Match(input,pattern)
            if r.Success then Some (r,f) else None
        ) 
        |> Seq.toList
    /// Execute input against step definitions
    let execute steps input =
        choose steps input
        |> function 
        | [(r,f)] -> 
            let ps =
                [for i = 1 to r.Groups.Count-1 do 
                    yield r.Groups.[i].Value]
            f ps 
        | [] -> invalidOp <| "Missing step definition for input " + input 
        | _::_ -> invalidOp <| "Multiple step definitions match input " + input
    /// Try step line against registered step definitions
    let tryStep (step,line:TickSpec.LineSource) =
        try
            match step with
            | GivenStep s -> execute givens s
            | WhenStep s -> execute whens s
            | ThenStep s -> execute thens s
            ConsoleColor.Green, ignore
        with 
        | Pending () -> 
            ConsoleColor.Yellow, ignore
        | e -> 
            ConsoleColor.Red, (fun () -> Console.WriteLine e)
        |> fun (color,detail) -> 
            Console.ForegroundColor <- color
            Console.WriteLine line.Text
            detail()       
            Console.ForegroundColor <- ConsoleColor.White
    let feature = FeatureParser.parseFeature lines
    Console.WriteLine feature.Name
    feature.Scenarios 
    |> Seq.iter (fun scenario -> 
        Console.WriteLine scenario.Name
        scenario.Steps |> Seq.iter tryStep
    )