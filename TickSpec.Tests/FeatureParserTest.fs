module TickSpec.Test.FeatureParserTest

open NUnit.Framework
open TickSpec
open System
open TickSpec.LineParser
open TickSpec.BlockParser
open System.Text

let private verifyParsing (fileContent: string) (expected: FeatureSource) =
    let featureSource = fileContent.Split([|Environment.NewLine|], StringSplitOptions.None) |> FeatureParser.parseFeature
    Assert.AreEqual(expected, featureSource)

let private verifyLineParsing (fileContent: string) (expected: LineType list) =
    let lines = fileContent.Split([|Environment.NewLine|], StringSplitOptions.None)
    let lineParsed =
        lines
        |> Seq.map (fun line ->
            let i = line.IndexOf("#")
            if i = -1 then line
            else line.Substring(0, i)
        )
        |> Seq.filter (fun line -> line.Trim().Length > 0)
        |> Seq.scan (fun (_, lastLine) line ->
            let parsed = parseLine (lastLine, line)
            match parsed with
            | Some line -> (lastLine, line)
            | None ->
                let e = expectingLine lastLine
                Exception(e) |> raise) (FileStart, FileStart)
        |> Seq.map (fun (_, line) -> line)

    Assert.AreEqual(expected, lineParsed)

let private verifyBlockParsing (fileContent: string) (expected: FeatureBlock) =
    let lines = fileContent.Split([|Environment.NewLine|], StringSplitOptions.None)
    let parsed = parseBlocks lines
    Assert.AreEqual(expected, parsed)

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
        Background
        Step (GivenStep "User connects to <server>")
        TagLine [ "basics"; "index" ]
        Scenario "Scenario Outline: Tags and Examples Sc."
        Step (WhenStep "Client requests <page>")
        Step (ThenStep "Server responds with page <page>")
        TagLine [ "smoke"; "all" ]
        Examples
        Item (Examples, TableRow [ "server" ])
        Item (Examples, TableRow [ "smoke" ])
        Examples
        Item (Examples, TableRow [ "page" ])
        Item (Examples, TableRow [ "index.html" ])
        Item (Examples, TableRow [ "default.html" ])
        TagLine [ "all" ]
        SharedExamples
        Item (SharedExamples, TableRow [ "server" ])
        Item (SharedExamples, TableRow [ "testing" ])
        Item (SharedExamples, TableRow [ "production" ])
    ]

[<Test>]
let TagsAndExamples_ParseBlocks () =
    tagsAndExamplesFeatureFile
    |> verifyBlockParsing <|
    {
        Name = "HTTP server"
        Tags = [ "http" ]
        Background = [
            {
                Step = GivenStep "User connects to <server>"
                LineNumber = 6
                LineString = "    Given User connects to <server>"
                Item = None
            }
        ]
        Scenarios = [
            {
                Name = "Scenario Outline: Tags and Examples Sc."
                Tags = [ "basics"; "index" ]
                Steps = [
                    {
                        Step = WhenStep "Client requests <page>"
                        LineNumber = 10
                        LineString = "    When Client requests <page>"
                        Item = None
                    }
                    {
                        Step = ThenStep "Server responds with page <page>"
                        LineNumber = 11
                        LineString = "    Then Server responds with page <page>"
                        Item = None
                    }
                ]
                Examples = [
                    {
                        Tags = [ "smoke"; "all" ]
                        Table =
                            {
                                Header = [ "server" ]
                                Rows = [ [ "smoke" ] ]
                            }
                        LineNumber = 14
                    }
                    {
                        Tags = []
                        Table =
                            {
                                Header = [ "page" ]
                                Rows = [ [ "index.html" ]; [ "default.html" ] ]
                            }
                        LineNumber = 18
                    }
                ]
            }
        ]
        SharedExamples = [
            {
                Tags = [ "all" ]
                Table =
                    {
                        Header = [ "server" ]
                        Rows = [ [ "testing" ]; [ "production" ] ]
                    }
                LineNumber = 24
            }
        ]
    }

[<Test>]
let TagsAndExamples_FeatureSource () =
    tagsAndExamplesFeatureFile
    |> verifyParsing <|
    {
        Name = "HTTP server"
        Scenarios = [|
            {
                Name = "Scenario Outline: Tags and Examples Sc. (1)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 6
                        Text = "    Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "    When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "    Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","index.html");("server","smoke")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples Sc. (2)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 6
                        Text = "    Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "    When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "    Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","default.html");("server","smoke")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples Sc. (3)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 6
                        Text = "    Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "    When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "    Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","index.html");("server","testing")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples Sc. (4)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 6
                        Text = "    Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "    When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "    Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","default.html");("server","testing")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples Sc. (5)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 6
                        Text = "    Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "    When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "    Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","index.html");("server","production")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples Sc. (6)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 6
                        Text = "    Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "    When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "    Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","default.html");("server","production")|]
            }
        |]
    }

let featureFileWithItems =
    "
    Feature: Items test feature
    Scenario: Items test scenario
    Given I have a table
        | col1  | col2  |
        | v11   | v21   |
        | v12   | v22   |
    When I take a doc string
        \"\"\"
        First line
           Second line
        Third line
        \"\"\"
    Then I can take a bullet list
        * First item
        * Second item
    "

let featureFileWithItems_expectedDocString =
    StringBuilder()
        .AppendLine("First line")
        .AppendLine("   Second line")
        .Append("Third line")
        .ToString()

[<Test>]
let FileWithItems_ParseLines () =
    featureFileWithItems
    |> verifyLineParsing <|
    [
        FileStart
        FeatureName "Items test feature"
        Scenario "Scenario: Items test scenario"
        Step (GivenStep "I have a table")
        Item (Step (GivenStep "I have a table"), TableRow [ "col1"; "col2" ])
        Item (Step (GivenStep "I have a table"), TableRow [ "v11"; "v21" ])
        Item (Step (GivenStep "I have a table"), TableRow [ "v12"; "v22" ])
        Step (WhenStep "I take a doc string")
        Item (Step (WhenStep "I take a doc string"), MultiLineStringStart 8)
        Item (Step (WhenStep "I take a doc string"), MultiLineString "        First line")
        Item (Step (WhenStep "I take a doc string"), MultiLineString "           Second line")
        Item (Step (WhenStep "I take a doc string"), MultiLineString "        Third line")
        Item (Step (WhenStep "I take a doc string"), MultiLineStringEnd)
        Step (ThenStep "I can take a bullet list")
        Item (Step (ThenStep "I can take a bullet list"), BulletPoint "First item")
        Item (Step (ThenStep "I can take a bullet list"), BulletPoint "Second item")
    ]

[<Test>]
let FileWithItems_ParseBlocks () =
    featureFileWithItems
    |> verifyBlockParsing <|
    {
        Name = "Items test feature"
        Tags = []
        Background = []
        Scenarios = [
            {
                Name = "Scenario: Items test scenario"
                Tags = []
                Steps = [
                    {
                        Step = GivenStep "I have a table"
                        LineNumber = 4
                        LineString = "    Given I have a table"
                        Item = Some (TableItem {
                            Header = [ "col1"; "col2" ]
                            Rows = [ [ "v11"; "v21" ]; [ "v12"; "v22" ] ]
                        })
                    }
                    {
                        Step = WhenStep "I take a doc string"
                        LineNumber = 8
                        LineString = "    When I take a doc string"
                        Item = Some (DocStringItem featureFileWithItems_expectedDocString)
                    }
                    {
                        Step = ThenStep "I can take a bullet list"
                        LineNumber = 14
                        LineString = "    Then I can take a bullet list"
                        Item = Some (BulletsItem [ "First item"; "Second item" ])
                    }
                ]
                Examples = []
            }
        ]
        SharedExamples = []
    }

[<Test>]
let FileWithItems_ParseFeature () =
    featureFileWithItems
    |> verifyParsing <|
    {
        Name = "Items test feature"
        Scenarios = [|
            {
                Name = "Scenario: Items test scenario"
                Tags = [||]
                Steps = [|
                    (GivenStep "I have a table", {
                        Number = 4
                        Text = "    Given I have a table"
                        Bullets = None
                        Table = Some (Table([| "col1"; "col2" |], [| [| "v11"; "v21" |]; [| "v12"; "v22" |] |]))
                        Doc = None
                    })
                    (WhenStep "I take a doc string", {
                        Number = 8
                        Text = "    When I take a doc string"
                        Bullets = None
                        Table = None
                        Doc = Some featureFileWithItems_expectedDocString
                    })
                    (ThenStep "I can take a bullet list", {
                        Number = 14
                        Text = "    Then I can take a bullet list"
                        Bullets = Some [| "First item"; "Second item" |]
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [||]
            }
        |]
    }