namespace TickSpec.NUnit

open TickSpec
open TickSpec.Functional
open NUnit.Framework

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
    open System.IO
    open System.Reflection
    let parse source =    
        let asm = Assembly.GetExecutingAssembly()
        let stream = asm.GetManifestResourceStream(source)
        use reader = new StreamReader(stream)
        let lines = reader |> TextReader.ReadAllLines
        FeatureParser.parseFeature(lines)

[<TestFixture;AbstractClass>]
type FeatureFixture<'TState> 
        (featureFile, 
         performStep:'TState->StepType->'TState,
         initState:unit->'TState) =
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.TestScenario (scenario:ScenarioSource) =
        scenario.Steps
        |> Array.map fst
        |> Array.fold performStep (initState())
        |> ignore
    member this.Scenarios =
        let feature = parse featureFile
        feature.Scenarios
        |> Seq.filter (fun scenario ->
            scenario.Tags |> Seq.exists ((=) "ignore") |> not
        )