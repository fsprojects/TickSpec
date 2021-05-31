module ByFramework.XUnit.Feature

open Xunit
open TickSpec.XunitWiring

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "Xunit.FSharp.Addition.feature")>]
let Addition (scenario : XunitSerializableScenario) = source.ScenarioAction(scenario).Invoke()

[<Theory; MemberData("scenarios", "Xunit.FSharp.Stock.feature")>]
let Stock (scenario : XunitSerializableScenario) = source.ScenarioAction(scenario).Invoke()