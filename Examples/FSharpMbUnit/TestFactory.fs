module Features
    
open System.Reflection
open TickSpec
open MbUnit.Framework

let assembly = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(assembly)       

[<StaticTestFactory>]
let TestFeatures () =        
    assembly.GetManifestResourceNames()
    |> Array.filter(fun name -> 
        name.EndsWith(".txt") || name.EndsWith(".feature")
    )
    |> Array.map (fun source ->                         
        let s = assembly.GetManifestResourceStream(source)           
        let feature = definitions.GenerateFeature(source,s)
        let suite = TestSuite(feature.Name)
        feature.Scenarios |> Seq.iter (fun scenario ->
            TestCase(
                scenario.ToString(), 
                Gallio.Common.Action(
                    fun () -> scenario.Action.Invoke()
                )
            )       
            |> suite.Children.Add
        )
        suite                       
    )