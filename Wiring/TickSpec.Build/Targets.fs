module TickSpec.Build.Targets

open System.IO

let GenerateTestFixtures (output:string) = 
    printfn $"Generating test fixtures '{output}' ..."

    let featuresFolder = Path.GetDirectoryName(output)

    let features = 
        featuresFolder 
        |> GherkinParser.FindAllFeatureFiles
        |> List.map GherkinParser.Read

    if features.Length = 0 then
        printfn "No feature files found in %s" featuresFolder
    else
        use writer = new StreamWriter(output)
        TestFixtureGenerator.Generate writer features

let GenerateHtmlDocs tocFormat (input:string) (output:string) =
    printfn $"Generating documenation for '{input}' ..."

    let stylesheet (feature:Feature) = 
        match tocFormat with
        | Some(HtmlGenerator.Html) ->
            (feature.Location.Folders |> List.map(fun _ -> ".."))@["style.css"]
            |> String.concat "/"
            |> Some
        | _ -> None

    let createFilePath feature =
        [
            [output]
            feature.Location.Folders
            [feature.Name + ".html"]
        ]
        |> List.concat
        |> Array.ofList
        |> Path.Combine

    let ensureFolderExists (file:string) =
        let folder = Path.GetDirectoryName(file)
        if folder |> Directory.Exists |> not then
            Directory.CreateDirectory(folder) |> ignore

    let generate (feature:Feature) =
        let file = feature |> createFilePath
        
        file |> ensureFolderExists

        use writer = new StreamWriter(file)
        HtmlGenerator.GenerateArticle writer (feature |> stylesheet) feature

    let features =
        input
        |> GherkinParser.FindAllFeatureFiles
        |> List.map GherkinParser.Read

    if features |> Seq.isEmpty |> not then
        features
        |> Seq.iter generate

        tocFormat
        |> Option.iter(fun f -> HtmlGenerator.GenerateToC f features output)

        printfn $"Documentation generated to '{output}'"
    else
        printfn $"No feature files found"

