module Fixtures

open NUnit.Framework
open System.Reflection
open TickSpec

let ass = Assembly.GetExecutingAssembly() 
let definitions = new StepDefinitions(ass)       

[<TestFixture>]
type Feature2 () =
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member this.TestScenario (scenario:Scenario) =
        scenario.Action.Invoke()        
    static member Scenarios =
        let source = @"Feature2.txt"
        let s = ass.GetManifestResourceStream(source)   
        definitions.GenerateScenarios(source,s)