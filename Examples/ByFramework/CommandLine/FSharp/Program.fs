module Program

open System.Reflection
open TickSpec

do  let ass = Assembly.GetExecutingAssembly()
    let definitions = new StepDefinitions(ass)

    [@"Feature.txt"]
    |> Seq.iter (fun source ->
        let s = ass.GetManifestResourceStream(source)
        definitions.Execute(source,s)
    )