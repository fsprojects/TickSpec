module ByFramework.XUnit2.Feature

open TickSpec
open Xunit

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "Xunit2.FSharp.Addition.feature")>]
let Addition (scenario : Scenario) = scenario.Action.Invoke()

[<Theory; MemberData("scenarios", "Xunit2.FSharp.Stock.feature")>]
let Stock (scenario : Scenario) = scenario.Action.Invoke()