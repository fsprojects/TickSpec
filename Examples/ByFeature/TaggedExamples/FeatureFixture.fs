module TickSpec.NUnit

open NUnit.Framework
open TickSpec

[<TestFixture>]
type FeatureFixture () =
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.TestScenario (scenario:Scenario) =
        if scenario.Tags |> Seq.contains "ignore" then
            raise (IgnoreException("Ignored: " + scenario.Name))
        scenario.Action.Invoke()

    static member Scenarios =
        let createTestCaseData (feature:Feature) (scenario:Scenario) =
            let testCaseData =
                (new TestCaseData(scenario))
                    .SetName(scenario.Name)
                    .SetProperty("Feature", feature.Name)

            scenario.Tags |> Seq.fold (fun (data:TestCaseData) (tag:string) -> data.SetProperty("Tag", tag)) testCaseData

        let createFeatureData (feature:Feature) =
            feature.Scenarios
            |> Seq.map (createTestCaseData feature)

        let assembly = typeof<FeatureFixture>.Assembly
        let definitions = new StepDefinitions(assembly)
        [
            "TaggedExamples.WebTesting.feature" ;
            "TaggedExamples.HttpServer.feature" ;
        ]
        |> Seq.collect ( fun source ->
            let featureStream = assembly.GetManifestResourceStream(source)
            let feature = definitions.GenerateFeature(source, featureStream)
            createFeatureData feature)