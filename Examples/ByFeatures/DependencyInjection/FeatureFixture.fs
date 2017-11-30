module TickSpec.NUnit

open System.Reflection
open NUnit.Framework
open TickSpec

let assembly = Assembly.GetExecutingAssembly()
let definitions = StepDefinitions(assembly)

/// Inherit from FeatureFixture to define a feature fixture
[<AbstractClass>]
[<TestFixture>]
type FeatureFixture (source:string) =
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.TestScenario (scenario:Scenario) =
        if scenario.Tags |> Seq.exists ((=) "ignore") then
            raise (IgnoreException("Ignored: " + scenario.Name))
        scenario.Action.Invoke()

    member this.Scenarios =
        let stream = assembly.GetManifestResourceStream(source)
        definitions.GenerateScenarios(source, stream)

type DependencyFixture () = inherit FeatureFixture("Dependency.feature")