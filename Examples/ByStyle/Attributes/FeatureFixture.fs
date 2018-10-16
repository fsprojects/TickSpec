module NUnit.TickSpec

open TickSpec
open NUnit.Framework

open System.Reflection
open System.Runtime.ExceptionServices

/// Class containing all BDD tests in current assembly as NUnit unit tests
[<TestFixture>]
type FeatureFixture () =
    /// Test method for all BDD tests in current assembly as NUnit unit tests
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member __.Bdd (scenario:Scenario) =
        if scenario.Tags |> Seq.exists ((=) "ignore") then
            raise (new IgnoreException("Ignored: " + scenario.ToString()))
        try
            scenario.Action.Invoke()
        with
        | :? TargetInvocationException as ex -> ExceptionDispatchInfo.Capture(ex.InnerException).Throw()

    /// All test scenarios from feature files in current assembly
    static member Scenarios =
        let createFeatureData (feature:Feature) =
            let createTestCaseData (feature:Feature) (scenario:Scenario) =
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
            |> Seq.map (createTestCaseData feature)
        
        let assembly = Assembly.GetExecutingAssembly()
        let definitions = new StepDefinitions(assembly.GetTypes())

        assembly.GetManifestResourceNames()
        |> Seq.filter (fun (n:string) -> n.EndsWith(".feature") )
        |> Seq.collect (fun n ->
            definitions.GenerateFeature(n, assembly.GetManifestResourceStream(n))
            |> createFeatureData)