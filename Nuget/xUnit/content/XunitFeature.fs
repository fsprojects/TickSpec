module XunitFeature

open TickSpec
open Xunit

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "PUTP-ROJECT-NAME-HERE.Stock.feature")>]
let Stock (scenario : Scenario) = scenario.Action.Invoke()