module NUnit.TickSpec

open TickSpec
open NUnit.Framework
open System

open System.Reflection
open System.Runtime.ExceptionServices

/// Class containing all BDD tests in current assembly as NUnit unit tests
[<TestFixture>]
type FeatureFixture () =
    /// Test method for all BDD tests in current assembly as NUnit unit tests
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.Bdd (scenario:Scenario) =
        if scenario.Tags |> Seq.exists ((=) "ignore") then
            raise (new IgnoreException("Ignored: " + scenario.ToString()))
        try
            scenario.Action.Invoke()
        with
        | :? TargetInvocationException as ex -> ExceptionDispatchInfo.Capture(ex.InnerException).Throw()

    /// All test scenarios from feature files in current assembly
    static member Scenarios =
        let assembly = Assembly.GetExecutingAssembly()
        let definitions = new StepDefinitions(assembly.GetTypes())
        let replaceParameterInScenarioName (scenarioName:string) parameter =
            scenarioName.Replace("<" + fst parameter + ">", snd parameter)
        let enhanceScenarioName parameters scenarioName =
            parameters
            |> Seq.fold replaceParameterInScenarioName scenarioName
        let createTestCaseData (feature:Feature) (scenario:Scenario) =
            (new TestCaseData(scenario))
                .SetName(enhanceScenarioName scenario.Parameters scenario.Name)
                .SetProperty("Feature", feature.Name.Substring(9))
            |> Seq.foldBack (fun (tag:string) data -> data.SetProperty("Tag", tag)) scenario.Tags
        let createFeatureData (feature:Feature) =
            feature.Scenarios
            |> Seq.map (createTestCaseData feature)

        assembly.GetManifestResourceNames()
        |> Seq.filter (fun (n:string) -> n.EndsWith(".feature") )
        |> Seq.map (fun n -> (n, assembly.GetManifestResourceStream(n)))
        |> Seq.map (fun (n, r) -> definitions.GenerateFeature(n,r))
        |> Seq.map createFeatureData
        |> Seq.concat