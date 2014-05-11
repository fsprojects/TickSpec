namespace TickSpec.Functional

open System.Reflection
open TickSpec

[<AutoOpen>]
module Exceptions =
    exception Pending of unit
    let pending () = raise <| Pending()
    let notImplemented () = raise <| new System.NotImplementedException()

[<AutoOpen>]
module Patterns =
    open System.Text.RegularExpressions
    let private Regex input pattern =
        let r = Regex.Match(input,pattern)
        if r.Success then Some [for i = 1 to r.Groups.Count-1 do yield r.Groups.[i].Value]
        else None
    let (|Given|_|) (pattern:string) (step) =
        match step with
        | GivenStep input -> Regex input pattern        
        | WhenStep _ | ThenStep _ -> None
    let (|When|_|) (pattern:string) (step) =
        match step with
        | WhenStep input -> Regex input pattern        
        | GivenStep _ | ThenStep _ -> None    
    let (|Then|_|) (pattern:string) (step) =
        match step with
        | ThenStep input -> Regex input pattern        
        | GivenStep _ | WhenStep _ -> None
    let (|Char|) (text:string) = text.[0]
    let (|Int|) s = System.Int32.Parse(s)

[<AutoOpen>]
module ConsoleRunner =
    open System
    open System.IO

    let tryStep performStep state (step,line) =
        let print color =
            let old = Console.ForegroundColor
            Console.ForegroundColor <- color
            printfn "%s" (line.Text.Trim())
            Console.ForegroundColor <- old
        try 
            let acc = performStep state step
            print ConsoleColor.Green
            acc
        with e ->
            print ConsoleColor.Red
            printfn "Line %d: %A" line.Number e
            reraise ()

    let run feature performStep initState =
        let lines = File.ReadAllLines(feature) 
        let feature = FeatureParser.parseFeature lines
        feature.Scenarios
        |> Seq.filter (fun scenario -> scenario.Tags |> Seq.exists ((=) "ignore") |> not) 
        |> Seq.iter (fun scenario ->
            scenario.Steps |> Array.scan (tryStep performStep) (initState())
            |> ignore
        )        
         