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
        scenario.Action.Invoke()        
    member this.Scenarios =       
        let s = ass.GetManifestResourceStream(source)   
        definitions.GenerateScenarios(source,s)
        |> Seq.filter (fun scenario ->
            scenario.Tags |> Seq.exists ((=) "ignore") |> not
        )