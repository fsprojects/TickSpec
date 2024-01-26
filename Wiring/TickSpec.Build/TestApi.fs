module TickSpec.Build.TestApi

open System.IO

[<AutoOpen>]
module private Impl =
    let getLocation (file:string) =
        let tokens = file.Split([| '/'; '\\'|])
        {
            Filename = tokens |> Seq.last
            Folders = tokens |> Seq.take (tokens.Length - 1) |> List.ofSeq
        }

let GenerateHtmlDoc (featureText:string) =
    use writer = new StringWriter()

    let location = "Dummy.feature" |> getLocation

    featureText
    |> GherkinParser.Parse location
    |> HtmlGenerator.GenerateArticle writer None

    writer.ToString()        

let GenerateTestFixtures file (featureText:string list) = 
    use writer = new StringWriter()

    let location = file |> getLocation

    featureText 
    |> List.map (GherkinParser.Parse location)
    |> TestFixtureGenerator.Generate writer

    writer.ToString()        
