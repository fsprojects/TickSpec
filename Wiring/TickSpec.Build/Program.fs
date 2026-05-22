module TickSpec.Build.Program

open System
open CommandLine

module Unions =
    open System.Reflection
    open Microsoft.FSharp.Reflection

    let private bindingFlags = BindingFlags.Public ||| BindingFlags.NonPublic
    
    let fromString<'a> (s:string) =
        try
            match FSharpType.GetUnionCases(typeof<'a>, bindingFlags) |> Array.filter (fun case -> case.Name.Equals(s,StringComparison.OrdinalIgnoreCase)) with
            |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
            |_ -> None
        with
            | _ -> None

[<Verb("fixtures", HelpText = "Generates code behind for test fixtures")>]
type FixturesOptions = {
    [<Value(0, MetaName="file", HelpText = "F# file the fixtures shall be generated into (feature files are collected from this folder)")>] File : string
}

[<Verb("doc", HelpText = "Generates HTML documentation of the feature files")>]
type DocOptions = {
    [<Value(0, MetaName="input", HelpText = "Folder to search for *.feature files")>] Input : string
    [<Value(1, MetaName="output", HelpText = "Folder to generate the HTML files to")>] Output : string
    [<Option('t', "toc", Required = false, HelpText = "Generate table of contents in given format (HTML, Json)")>] TocFormat : string option
}

[<AutoOpen>]
module private Impl =
    let runFixtures opts =
        if String.IsNullOrEmpty(opts.File) then
            failwith "Name of the file to generate missing"

        Targets.GenerateTestFixtures opts.File

        0

    let runDoc opts = 
        if String.IsNullOrEmpty(opts.Input) then
            failwith "Folder to scan for feature files missing"

        if String.IsNullOrEmpty(opts.Output) then
            failwith "Output folder missing"

        let tocFormat = opts.TocFormat |> Option.bind Unions.fromString<HtmlGenerator.TocFormat>
        Targets.GenerateHtmlDocs tocFormat opts.Input opts.Output
        0

    let printErrors (errors:Error seq) =
        let help = new Text.HelpText()
        errors
        |> Seq.filter(fun x -> x.Tag <> ErrorType.HelpRequestedError)
        |> Seq.filter(fun x -> x.Tag <> ErrorType.HelpVerbRequestedError)
        |> Seq.filter(fun x -> x.Tag <> ErrorType.VersionRequestedError)
        |> Seq.map(fun x -> x |> printf "%A"; help.SentenceBuilder.FormatError.Invoke(x))
        |> Seq.iter Console.Error.WriteLine
        1

[<EntryPoint>]
let main args =

    try
        let parser = new Parser(fun settings ->
            settings.AutoHelp <- true
            settings.AutoVersion <- false
            settings.CaseSensitive <- false
            settings.CaseInsensitiveEnumValues <- true
            settings.HelpWriter <- Console.Out
        )

        let result = parser.ParseArguments<FixturesOptions, DocOptions> args
        result.MapResult(runFixtures, runDoc, printErrors)
    with
        | ex -> 
            Console.Error.WriteLine($"ERROR: {ex.Message}")
            1
