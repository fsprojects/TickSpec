module TickSpec.Test.FeatureParserTest

open NUnit.Framework
open TickSpec
open System
open TickSpec.LineParser

let private verifyParsing (fileContent: string) (expected: FeatureSource) =
    let featureSource = fileContent.Split([|"\n"|], StringSplitOptions.None) |> FeatureParser.parseFeature
    Assert.AreEqual(expected, featureSource)

let private verifyLineParsing (fileContent: string) (expected: LineType list) =
    let lines = fileContent.Split([|"\n"|], StringSplitOptions.None)
    let lineParsed =
        lines
        |> Seq.map (fun line ->
            let i = line.IndexOf("#")
            if i = -1 then line
            else line.Substring(0, i)
        )
        |> Seq.filter (fun line -> line.Trim().Length > 0)
        |> Seq.scan (fun (_, lastLine) line ->
            let parsed = TickSpec.LineParser.parseLine (lastLine, line)
            match parsed with
            | Some line -> (lastLine, line)
            | None ->
                let e = expectingLine lastLine
                Exception(e) |> raise) (FileStart, FileStart)
        |> Seq.map (fun (_, line) -> line)

    Assert.AreEqual(expected, lineParsed)

let tagsAndExamplesFeatureFile =
    "
    @http
    Feature: HTTP server

    Background:
    Given User connects to <server>

    @basics @index
    Scenario Outline: Tags and Examples Sc.
    When Client requests <page>
    Then Server responds with page <page>

    @smoke @all
    Examples:
        | server  |
        | smoke   |

    Examples:
        | page         |
        | index.html   |
        | default.html |

    @all
    Shared Examples:
        | server     |
        | testing    |
        | production |
    "

[<Test>]
let TagsAndExamples_ParseLines () =
    tagsAndExamplesFeatureFile
    |> verifyLineParsing <|
    [
        FileStart
        TagLine [ "http" ]
        FeatureName "HTTP server"
        BlockStart Background
        Step (GivenStep "User connects to <server>")
        TagLine [ "basics"; "index" ]
        BlockStart (Named "Tags and Examples Sc.")
        Step (WhenStep "Client requests <page>")
        Step (ThenStep "Server responds with page <page>")
        TagLine [ "smoke"; "all" ]
        ExamplesStart
        Item (ExamplesStart, TableRow [| "server" |])
        Item (ExamplesStart, TableRow [| "smoke" |])
        ExamplesStart
        Item (ExamplesStart, TableRow [| "page" |])
        Item (ExamplesStart, TableRow [| "index.html" |])
        Item (ExamplesStart, TableRow [| "default.html" |])
        TagLine [ "all" ]
        BlockStart (Shared None)
        Item (BlockStart (Shared None), TableRow [| "server" |])
        Item (BlockStart (Shared None), TableRow [| "testing" |])
        Item (BlockStart (Shared None), TableRow [| "production" |])
    ]

[<Test>]
let TagsAndExamples_FeatureSource () =
    tagsAndExamplesFeatureFile
    |> verifyParsing <|
    {
        Name = "HTTP server"
        Scenarios = [|
            {
                Name = "Tags and Examples Sc. (0)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","smoke");("page","index.html")|]
            }
            {
                Name = "Tags and Examples Sc. (1)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","smoke");("page","default.html")|]
            }
            {
                Name = "Tags and Examples Sc. (2)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","testing");("page","index.html")|]
            }
            {
                Name = "Tags and Examples Sc. (3)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","testing");("page","default.html")|]
            }
            {
                Name = "Tags and Examples Sc. (4)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","production");("page","index.html")|]
            }
            {
                Name = "Tags and Examples Sc. (5)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","production");("page","default.html")|]
            }
        |]
    }