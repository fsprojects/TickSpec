module TickSpec.Build.TestFixtureGenerator

open System.IO

[<AutoOpen>]
module private Impl =
    let writeHeader (writer:TextWriter) =
        writer.WriteLine("namespace Specification");
        writer.WriteLine()
        writer.WriteLine("open System.Reflection")
        writer.WriteLine("open NUnit.Framework")
        writer.WriteLine("open TickSpec.CodeGen")
        writer.WriteLine()

    let writeTestCase (writer:TextWriter) location scenario =
        let file = location.Folders@[location.Filename] |> String.concat "/"
        writer.WriteLine($"    [<Test>]")
        writer.WriteLine($"    member this.``{scenario.Title}``() =")
        writer.WriteLine($"#line {scenario.StartsAtLine} \"{file}\"")
        writer.WriteLine($"        this.RunScenario(scenarios, \"{scenario.Name}\")")
        writer.WriteLine()

    let writeTestFixture (writer:TextWriter) feature =
        let resourceId = feature.Location.Folders@[feature.Location.Filename] |> String.concat "." 
        writer.WriteLine($"[<TestFixture>]")
        writer.WriteLine($"type ``{feature.Name}``() = ")
        writer.WriteLine($"    inherit AbstractFeature()")
        writer.WriteLine()
        writer.WriteLine($"    let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), \"{resourceId}\")")
        writer.WriteLine()

        feature.Scenarios
        |> Seq.iter (writeTestCase writer feature.Location)

let Generate (writer:TextWriter) features =
    
    writeHeader writer

    features
    |> Seq.iter (writeTestFixture writer)
