module Program

open System.Reflection
open TickSpec

let ass = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(ass)

[<TickFact>]
let Feature1 () =
    let source = @"Feature2.txt"
    let s = ass.GetManifestResourceStream(source)   
    definitions.GenerateScenarios(source,s)   