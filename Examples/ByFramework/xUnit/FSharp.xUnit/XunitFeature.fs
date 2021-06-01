module ByFramework.XUnit.Feature

open global.Xunit
open TickSpec.Xunit

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "Xunit.FSharp.Addition.feature")>]
let Addition (scenario : XunitSerializableScenario) = source.ScenarioAction(scenario).Invoke()

[<Theory; MemberData("scenarios", "Xunit.FSharp.Stock.feature")>]
let Stock (scenario : XunitSerializableScenario) = source.ScenarioAction(scenario).Invoke()