module ByFramework.Xunit1.Feature

open System.Reflection
open TickSpec

let ass = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(ass)

[<TickFact>]
let Stock() =
    let source = @"Stock.feature"
    let s = ass.GetManifestResourceStream(source)
    definitions.GenerateScenarios(source,s)

[<TickFact>]
let Addition() =
    let source = @"Addition.feature"
    let s = ass.GetManifestResourceStream(source)
    definitions.GenerateScenarios(source,s)