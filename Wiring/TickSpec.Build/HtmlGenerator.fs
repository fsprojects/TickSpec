module TickSpec.Build.HtmlGenerator

open System
open System.IO
open System.Xml.Linq
open System.Xml

type TocFormat =
    | Html
    | Json

type TocEntry = { 
    Title: string
    Folders : string list
    Filename: string 
}

[<AutoOpen>]
module private Impl = 
    open System.Text.Json

    let (|Keyword|_|) (keyword:string) (line:string) =
        if line.TrimStart().StartsWith(keyword + " ", StringComparison.OrdinalIgnoreCase) then
            let indent = line |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length
            (keyword.PadLeft(indent, ' '), line.Substring(keyword.Length + indent)) |> Some
         else
            None

    let generateStep (line:string) =
        match line with
        | Keyword "Given" (k,l) 
        | Keyword "When" (k,l)  
        | Keyword "Then" (k,l) 
        | Keyword "And" (k,l)  
        | Keyword "But" (k,l) -> 
            [
                new XElement("span", new XAttribute("class", "gherkin-keyword"), k) :> obj
                l
                Environment.NewLine
            ]
        | _ -> 
            [
                line
                Environment.NewLine
            ]

    let generateScenarioBody (lines:string list) =
        new XElement("pre", 
            new XAttribute("class", "gherkin-scenario-body"), 
            new XElement("code",
                lines
                |> Seq.map generateStep))

    let generateScenario (scenario:Scenario) =
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario"))

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            scenario.Title :> obj |]))

        match scenario.Tags with
        | [] -> ()
        | tags ->
            new XElement("div", 
                new XElement("span", new XAttribute("class", "gherkin-tags"), "Tags:"), 
                String.Join(", ", tags))
            |> doc.Add

        match scenario.Description with
        | "" -> ()
        | text ->
            doc.Add(new XElement("div", new XAttribute("class", "gherkin-description"), text))

        doc.Add(generateScenarioBody scenario.Body)

        doc
        
    let generateBackground (lines:string list) =
        let doc = new XElement("div", new XAttribute("class", "gherkin-scenario"))

        doc.Add(new XElement("h3", [|
            new XAttribute("class", "gherkin-scenario-title") :> obj
            "Background" :> obj |]))

        doc.Add(generateScenarioBody lines)

        doc

    let generateFeature stylesheet (feature:Feature) =
        let doc = new XElement("article")

        stylesheet
        |> Option.iter(fun (x:string) ->
            doc.Add(new XElement("link",
                new XAttribute("rel", "stylesheet"),
                new XAttribute("href", x))))

        doc.Add(new XElement("h2", [|
            new XAttribute("class", "gherkin-feature-title") :> obj
            feature.Name |]))

        match feature.Background with
        | [] -> ()
        | x -> doc.Add(generateBackground x)

        feature.Scenarios
        |> Seq.map generateScenario
        |> Seq.iter doc.Add

        doc

    let write (writer:TextWriter) (doc:XElement) =
        let settings = new XmlWriterSettings()
        // explicitly disable so that <pre/> formatting is kept
        settings.Indent <- false

        use xmlWriter = XmlWriter.Create(writer, settings)
        doc.WriteTo(xmlWriter)

    let generateHtmlToc (features:Feature list) =
        let head = new XElement("head",
            new XElement("link",
                new XAttribute("rel", "stylesheet"),
                new XAttribute("href", "style.css")))
        
        let body = 
            let doc = new XElement("body")
            doc.Add(new XElement("h2", "Table of contents"))

            features
            |> Seq.map(fun x -> [x.Name + ".html"] |> List.append x.Location.Folders |> String.concat "/",x)
            |> Seq.sortBy fst
            |> Seq.map(fun (file,x) ->
                new XElement("li", 
                    new XElement("a",[|
                        new XAttribute("href", file) :> obj
                        new XAttribute("target", "article") :> obj
                        x.Name |])))
            |> fun x -> doc.Add(new XElement("ul", x))

            doc.Add(new XElement("iframe",
                new XAttribute("id", "article"),
                new XAttribute("name", "article"),
                new XAttribute("width", "100%"),
                new XAttribute("height", "80%"),
                new XElement("div")))

            doc

        new XElement("html", head, body)

    let generateJsonToc (writer:TextWriter) (features:Feature list) =
        let entries =
            features
            |> Seq.map(fun x -> 
                { 
                    Title = x.Name
                    Folders = x.Location.Folders
                    Filename = x.Name + ".html" 
                })
            |> List.ofSeq

        let options = new JsonSerializerOptions()
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        options.WriteIndented <- true

        let json = JsonSerializer.Serialize(entries, options)
        writer.WriteLine(json)

let GenerateArticle (writer:TextWriter) stylesheet (feature:Feature) =
    feature
    |> generateFeature stylesheet
    |> write writer

let GenerateToC tocFormat (features:Feature list) (output:string) =
    match tocFormat with
    | Html -> 
        use writer = new StreamWriter(Path.Combine(output, "toc.html"))
        generateHtmlToc features |> write writer
    | Json -> 
        use writer = new StreamWriter(Path.Combine(output, "toc.json"))
        generateJsonToc writer features 
