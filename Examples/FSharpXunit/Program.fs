module Program

open System.Reflection
open TickSpec

let ass = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(ass)

[<TickFact>]
let Feature1 () =
    let source = @"FeatureX.txt"
    let s = ass.GetManifestResourceStream(source)   
    definitions.GenerateScenarios(source,s)   

[<TickFact>]
let Feature2 () =
    let source = @"AdditionX.txt"
    let s = ass.GetManifestResourceStream(source)   
    definitions.GenerateScenarios(source,s)   