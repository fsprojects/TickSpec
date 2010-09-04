module Program

open System.Reflection
open TickSpec

do  let ass = Assembly.GetExecutingAssembly()
    let definitions = new StepDefinitions(ass)

    let source = @"Feature.txt"
    let s = ass.GetManifestResourceStream(source)
    definitions.Execute(source,s)