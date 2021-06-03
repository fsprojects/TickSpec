module ByFramework.XUnit.Feature

open TickSpec.Xunit
open global.Xunit

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "Xunit.FSharp.Addition.feature")>]
let Addition (scenario : XunitSerializableScenario) = source.RunScenario(scenario)

[<Theory; MemberData("scenarios", "Xunit.FSharp.Stock.feature")>]
let Stock (scenario : XunitSerializableScenario) = source.RunScenario(scenario)