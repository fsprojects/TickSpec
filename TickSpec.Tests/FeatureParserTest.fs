module TickSpec.Test.FeatureParserTest

open NUnit.Framework
open TickSpec
open System
open TickSpec.LineParser
open TickSpec.BlockParser
open System.Text
open System.Reflection
open System.IO

let private verifyParsing (lines: string[]) (expected: FeatureSource) =
    let featureSource = lines |> FeatureParser.parseFeature
    Assert.AreEqual(expected, featureSource)

let private verifyLineParsing (lines: string[]) (expected: LineType list) =
    let lineParsed =
        lines
        |> Seq.mapi (fun lineNumber line -> (lineNumber + 1, line))
        |> Seq.map (fun (lineNumber, line) ->
            let i = line.IndexOf("#")
            if i = -1 then lineNumber, line
            else lineNumber, line.Substring(0, i)
        )
        |> Seq.scan (fun prevLine (lineNumber, lineContent) ->
            let lastParsedLine =
                match prevLine with
                | Line (_, _, prevLineType) -> prevLineType
                | IgnoredLine prevLineType -> prevLineType

            let parsed = parseLine (lastParsedLine, lineContent)
            match parsed with
            | Some line -> (lineNumber, lineContent, line) |> Line
            | None when lineContent.Trim().Length = 0 -> lastParsedLine |> IgnoredLine
            | None ->
                let e = expectingLine lastParsedLine
                Exception(e) |> raise) (Line (0, "", FileStart))
        |> Seq.choose (function
            | IgnoredLine _ -> None
            | Line (lineNumber, lineContent, lineType) -> Some (lineNumber, lineContent, lineType)
        )
        |> Seq.map (fun (_, _, line) -> line)

    Assert.AreEqual(expected, lineParsed)

let private verifyBlockParsing (lines: string[]) (expected: FeatureBlock) =
    let parsed = parseBlocks lines
    Assert.AreEqual(expected, parsed)

let private loadFeatureFile filePath =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath)
    use reader = new StreamReader(stream)
    reader |> TextReader.readAllLines

[<Test>]
let TagsAndExamples_ParseLines () =
    "TickSpec.Tests.TagsAndExamples.feature"
    |> loadFeatureFile
    |> verifyLineParsing <|
    [
        FileStart
        TagLine [ "http" ]
        FeatureName "HTTP server"
        Background
        Step (GivenStep "User connects to <server>")
        TagLine [ "basics"; "index" ]
        Scenario "Scenario Outline: Tags and Examples"
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
    "TickSpec.Tests.TagsAndExamples.feature"
    |> loadFeatureFile
    |> verifyBlockParsing <|
    {
        Name = "HTTP server"
        Tags = [ "http" ]
        Background = [
            {
                Step = GivenStep "User connects to <server>"
                LineNumber = 5
                LineString = "Given User connects to <server>"
                Item = None
            }
        ]
        Scenarios = [
            {
                Name = "Scenario Outline: Tags and Examples"
                Tags = [ "basics"; "index" ]
                Steps = [
                    {
                        Step = WhenStep "Client requests <page>"
                        LineNumber = 9
                        LineString = "When Client requests <page>"
                        Item = None
                    }
                    {
                        Step = ThenStep "Server responds with page <page>"
                        LineNumber = 10
                        LineString = "Then Server responds with page <page>"
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
                        LineNumber = 13
                    }
                    {
                        Tags = []
                        Table =
                            {
                                Header = [ "page" ]
                                Rows = [ [ "index.html" ]; [ "default.html" ] ]
                            }
                        LineNumber = 17
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
                LineNumber = 23
            }
        ]
    }

[<Test>]
let TagsAndExamples_FeatureSource () =
    "TickSpec.Tests.TagsAndExamples.feature"
    |> loadFeatureFile
    |> verifyParsing <|
    {
        Name = "HTTP server"
        Scenarios = [|
            {
                Name = "Scenario Outline: Tags and Examples (1)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 5
                        Text = "Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 9
                        Text = "When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 10
                        Text = "Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","index.html");("server","smoke")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples (2)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 5
                        Text = "Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 9
                        Text = "When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 10
                        Text = "Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","default.html");("server","smoke")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples (3)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 5
                        Text = "Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 9
                        Text = "When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 10
                        Text = "Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","index.html");("server","testing")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples (4)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 5
                        Text = "Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 9
                        Text = "When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 10
                        Text = "Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","default.html");("server","testing")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples (5)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 5
                        Text = "Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 9
                        Text = "When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 10
                        Text = "Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","index.html");("server","production")|]
            }
            {
                Name = "Scenario Outline: Tags and Examples (6)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 5
                        Text = "Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 9
                        Text = "When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 10
                        Text = "Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("page","default.html");("server","production")|]
            }
        |]
    }

let featureFileWithItems_expectedDocString =
    StringBuilder()
        .AppendLine("First line")
        .AppendLine()
        .AppendLine("    Second line")
        .Append("Third line")
        .ToString()

[<Test>]
let FileWithItems_ParseLines () =
    "TickSpec.Tests.WithItems.feature"
    |> loadFeatureFile
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
        Item (Step (WhenStep "I take a doc string"), MultiLineStringStart 4)
        Item (Step (WhenStep "I take a doc string"), MultiLineString "    First line")
        Item (Step (WhenStep "I take a doc string"), MultiLineString "")
        Item (Step (WhenStep "I take a doc string"), MultiLineString "        Second line")
        Item (Step (WhenStep "I take a doc string"), MultiLineString "    Third line")
        Item (Step (WhenStep "I take a doc string"), MultiLineStringEnd)
        Step (ThenStep "I can take a bullet list")
        Item (Step (ThenStep "I can take a bullet list"), BulletPoint "First item")
        Item (Step (ThenStep "I can take a bullet list"), BulletPoint "Second item")
        Step (ThenStep "Even the next step is clear")
    ]

[<Test>]
let FileWithItems_ParseBlocks () =
    "TickSpec.Tests.WithItems.feature"
    |> loadFeatureFile
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
                        LineNumber = 3
                        LineString = "Given I have a table"
                        Item = Some (TableItem {
                            Header = [ "col1"; "col2" ]
                            Rows = [ [ "v11"; "v21" ]; [ "v12"; "v22" ] ]
                        })
                    }
                    {
                        Step = WhenStep "I take a doc string"
                        LineNumber = 7
                        LineString = "When I take a doc string"
                        Item = Some (DocStringItem featureFileWithItems_expectedDocString)
                    }
                    {
                        Step = ThenStep "I can take a bullet list"
                        LineNumber = 14
                        LineString = "Then I can take a bullet list"
                        Item = Some (BulletsItem [ "First item"; "Second item" ])
                    }
                    {
                        Step = ThenStep "Even the next step is clear"
                        LineNumber = 17
                        LineString = "And Even the next step is clear"
                        Item = None
                    }
                ]
                Examples = []
            }
        ]
        SharedExamples = []
    }

[<Test>]
let FileWithItems_ParseFeature () =
    "TickSpec.Tests.WithItems.feature"
    |> loadFeatureFile
    |> verifyParsing <|
    {
        Name = "Items test feature"
        Scenarios = [|
            {
                Name = "Scenario: Items test scenario"
                Tags = [||]
                Steps = [|
                    (GivenStep "I have a table", {
                        Number = 3
                        Text = "Given I have a table"
                        Bullets = None
                        Table = Some (Table([| "col1"; "col2" |], [| [| "v11"; "v21" |]; [| "v12"; "v22" |] |]))
                        Doc = None
                    })
                    (WhenStep "I take a doc string", {
                        Number = 7
                        Text = "When I take a doc string"
                        Bullets = None
                        Table = None
                        Doc = Some featureFileWithItems_expectedDocString
                    })
                    (ThenStep "I can take a bullet list", {
                        Number = 14
                        Text = "Then I can take a bullet list"
                        Bullets = Some [| "First item"; "Second item" |]
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Even the next step is clear", {
                        Number = 17
                        Text = "And Even the next step is clear"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [||]
            }
        |]
    }

let placeholdersInItems_expectedDocString placeholder =
    StringBuilder()
        .AppendLine("First line with placeholder " + placeholder)
        .AppendLine("    Second line")
        .Append("Third line")
        .ToString()
    
[<Test>]
let PlaceholdersInItems_ParseFeature () =
    "TickSpec.Tests.PlaceholdersInItems.feature"
    |> loadFeatureFile
    |> verifyParsing <|
    {
        Name = "Placeholders in items feature"
        Scenarios = [|
            {
                Name = "Scenario Outline: Placeholders in items test scenario (1)"
                Tags = [||]
                Steps = [|
                    (GivenStep "I have a table with placeholders", {
                        Number = 3
                        Text = "Given I have a table with placeholders"
                        Bullets = None
                        Table = Some (Table([| "col1" |], [| [| "Value1" |] |]))
                        Doc = None
                    })
                    (WhenStep "I take a doc string with placeholders", {
                        Number = 6
                        Text = "When I take a doc string with placeholders"
                        Bullets = None
                        Table = None
                        Doc = placeholdersInItems_expectedDocString "Value1" |> Some
                    })
                    (ThenStep "I can even have a bullet list with placeholders", {
                        Number = 12
                        Text = "Then I can even have a bullet list with placeholders"
                        Bullets = Some [| "First item with placeholder Value1"; "Second item" |]
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("Placeholder1", "Value1")|]
            }
            {
                Name = "Scenario Outline: Placeholders in items test scenario (2)"
                Tags = [||]
                Steps = [|
                    (GivenStep "I have a table with placeholders", {
                        Number = 3
                        Text = "Given I have a table with placeholders"
                        Bullets = None
                        Table = Some (Table([| "col1" |], [| [| "Value2" |] |]))
                        Doc = None
                    })
                    (WhenStep "I take a doc string with placeholders", {
                        Number = 6
                        Text = "When I take a doc string with placeholders"
                        Bullets = None
                        Table = None
                        Doc = placeholdersInItems_expectedDocString "Value2" |> Some
                    })
                    (ThenStep "I can even have a bullet list with placeholders", {
                        Number = 12
                        Text = "Then I can even have a bullet list with placeholders"
                        Bullets = Some [| "First item with placeholder Value2"; "Second item" |]
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("Placeholder1", "Value2")|]
            }

        |]
    }        