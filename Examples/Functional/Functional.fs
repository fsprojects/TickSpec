open NUnit.Framework
open System.Reflection
open TickSpec

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
    
[<AutoOpen>]
module Parser =
    let parse source =    
        let ass = Assembly.GetExecutingAssembly()
        let stream = ass.GetManifestResourceStream(source)
        use reader = new System.IO.StreamReader(stream)
        let lines = reader |> TextReader.ReadAllLines
        TickSpec.FeatureParser.parseFeature(lines)

[<AutoOpen>]
module Exceptions =
    exception Pending of unit
    let pending () = raise <| Pending()
    let notImplemented () = raise <| new System.NotImplementedException()

[<AutoOpen>]
module Patterns =
    open System.Text.RegularExpressions
    let Regex input pattern =
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
    let (|Int|) s = System.Int32.Parse(s)

type Calculator = { Values : int list } with
    member this.Push n = {Values=n::this.Values}
    member this.Add() = {Values=[List.sum this.Values]}
    member this.Top = List.head this.Values
    static member Create() = { Values = [] }

module AdditionSteps =
    let performStep (calc:Calculator) (step,line:LineSource) =
        match step with
        | Given "I have entered (.*) into the calculator" [Int n] ->
            calc.Push n                        
        | When "I press add" [] -> 
            calc.Add ()
        | Then "the result should be (.*) on the screen" [Int n] ->
            Assert.AreEqual(n,calc.Top)
            calc            
        | _ -> sprintf "Unmatched line %d" line.Number |> invalidOp                 

module Runner =
    do  // TODO: colouring?
        let feature = parse "Addition.txt"
        feature.Scenarios
        |> Seq.filter (fun scenario -> scenario.Tags |> Seq.exists ((=) "ignore") |> not)       
        |> Seq.iter (fun scenario ->
            scenario.Steps
            |> Array.scan AdditionSteps.performStep (Calculator.Create ())            
            |> ignore
        )
        
[<TestFixture>]
type AdditionFixture () =
    [<Test>]
    [<TestCaseSource(typeof<ScenarioSource>,"Scenarios")>]
    member this.TestScenario (scenario:ScenarioSource) =
        scenario.Steps |> Seq.scan AdditionSteps.performStep (Calculator.Create())
        |> ignore  
    member this.Scenarios =       
        let feature = parse "Addition.txt"        
        feature.Scenarios
        |> Seq.filter (fun scenario ->
            scenario.Tags |> Seq.exists ((=) "ignore") |> not
        )
        