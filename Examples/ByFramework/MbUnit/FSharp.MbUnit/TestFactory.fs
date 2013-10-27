module Features
    
open System.Reflection
open TickSpec
open MbUnit.Framework

let assembly = Assembly.GetExecutingAssembly()
let definitions = new StepDefinitions(assembly)       

[<StaticTestFactory>]
let TestFeatures () =        
    let toTestCase scenario =
        TestCase(
            scenario.ToString(), 
            Gallio.Common.Action(
                fun () -> scenario.Action.Invoke()
            )
        )    
    assembly.GetManifestResourceNames()
    |> Array.filter(fun name -> 
        name.EndsWith(".txt") || name.EndsWith(".feature")
    )
    |> Array.map (fun source ->                         
        let s = assembly.GetManifestResourceStream(source)           
        let feature = definitions.GenerateFeature(source,s)
        let suite = TestSuite(feature.Name)
        feature.Scenarios
        |> Seq.filter (fun scenario ->
            scenario.Tags |> Seq.exists ((=) "ignore") |> not
        )
        |> Seq.groupBy (fun scenario -> scenario.Name)
        |> Seq.iter (fun (name,scenarios) ->            
            if Seq.length scenarios = 1 then
                scenarios |> Seq.head |> toTestCase 
                |> suite.Children.Add
            else
                let suite' = TestSuite(name)
                scenarios 
                |> Seq.map toTestCase
                |> Seq.iter suite'.Children.Add
                suite' |> suite.Children.Add                           
        )
        suite                       
    )