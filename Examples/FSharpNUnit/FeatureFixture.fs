module NUnit.TickSpec

open NUnit.Framework
open System.Reflection
open TickSpec

let ass = Assembly.GetExecutingAssembly() 
let definitions = new StepDefinitions(ass)       

/// Inherit from FeatureFixture to define a feature fixture
[<AbstractClass>]
[<TestFixture>]
type FeatureFixture (source:string) =
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.TestScenario (scenario:Scenario) =
        if scenario.Tags |> Seq.exists ((=) "ignore") then
            raise (new IgnoreException("Ignored: " + scenario.Name))
        scenario.Action.Invoke()
    member this.Scenarios =       
        let s = ass.GetManifestResourceStream(source)   
        definitions.GenerateScenarios(source,s)