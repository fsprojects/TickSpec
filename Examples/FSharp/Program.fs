module Program

open System.Reflection
open TickSpec

do   let ass = Assembly.GetExecutingAssembly()
     let definitions = new StepDefinitions(ass)

     let s = ass.GetManifestResourceStream(@"Feature.txt")
     definitions.Execute(s)