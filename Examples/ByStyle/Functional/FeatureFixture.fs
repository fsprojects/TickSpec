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

[<AbstractClass>]
type FeatureFixture<'TState> () =
    static member PerformTest (scenario:ScenarioSource) (performStep:'TState->StepType->'TState) (initState:unit->'TState) =
        scenario.Steps
        |> Array.map fst
        |> Array.fold performStep (initState())
        |> ignore
    static member MakeScenarios featureFile =
        let feature = parse featureFile
        let createTestCaseData (feature:FeatureSource) (scenario:ScenarioSource) =
                let enhanceScenarioName parameters scenarioName =
                    let replaceParameterInScenarioName (scenarioName:string) parameter =
                        scenarioName.Replace("<" + fst parameter + ">", snd parameter)
                    parameters
                    |> Seq.fold replaceParameterInScenarioName scenarioName
                (new TestCaseData(scenario))
                    .SetName(enhanceScenarioName scenario.Parameters scenario.Name)
                    .SetProperty("Feature", feature.Name.Substring(9))
                |> Seq.foldBack (fun (tag:string) data -> data.SetProperty("Tag", tag)) scenario.Tags
        feature.Scenarios
        |> Seq.filter (fun scenario ->
            scenario.Tags |> Seq.exists ((=) "ignore") |> not)
        |> Seq.map (createTestCaseData feature)
