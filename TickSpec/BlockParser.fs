module internal TickSpec.BlockParser

open TickSpec.LineParser
open System
open System.Text

let private raiseParseException message lines =
    match lines with
    | (lineNumber,_,_) :: _ ->
        let exMessage = sprintf "Parsing failed on row %d: %s" lineNumber message
        ParseException(exMessage, Some lineNumber) |> raise
    | _ ->
        let exMessage = sprintf "Parsing failed on the end of file: %s" message
        ParseException(exMessage, None) |> raise

type internal FeatureBlock =
    {
        Name: string
        Tags: string list
        Background: StepBlock list
        Scenarios: ScenarioBlock list
        SharedExamples: ExampleBlock list
    }
and internal ScenarioBlock =
    {
        Name: string
        Tags: string list
        Steps: StepBlock list
        Examples: ExampleBlock list
    }
and internal StepBlock =
    {
        Step: StepType
        LineNumber: int
        LineString: string
        Item: ItemBlock option
    }
and internal ItemBlock =
    | BulletsItem of string list
    | TableItem of TableBlock
    | DocStringItem of string
and internal TableBlock =
    {
        Header: string list
        Rows: string list list
    }
and internal ExampleBlock =
    {
        Tags: string list
        Table: TableBlock
        LineNumber: int
    }

let private parseTags lines =
    let rec parseTagsInternal tags lines =
        match lines with
        | (_,_,TagLine strings) :: xs -> parseTagsInternal (tags @ strings) xs
        | _ -> tags, lines
    parseTagsInternal [] lines

let parseTable origLines =
    let rec readTableRows rows lines =
        match lines with
        | (_, _, Item(_, TableRow cells)) :: xs ->
            readTableRows (rows @ [ cells ]) xs
        | _ -> rows, lines

    let allRows, lines = readTableRows [] origLines
    match allRows with
    | header :: rows -> { Header = header; Rows = rows }, lines
    | _ -> lines |> raiseParseException "Table expected"

let private parseExamples lines =
    let rec parseExamplesInternal examples origLines =
        let tags, lines = parseTags origLines

        match lines with
        | (lineNumber, _, Examples) :: xs ->
            let table, lines = parseTable xs
            parseExamplesInternal (examples @ [ { Tags = tags; Table = table; LineNumber = lineNumber }]) lines
        | _ -> examples, origLines

    parseExamplesInternal [] lines

let private parseSharedExamples lines =
    let rec parseSharedExamplesInternal examples origLines =
        let tags, lines = parseTags origLines

        match lines with
        | (lineNumber, _, SharedExamples) :: xs ->
            let table, lines = parseTable xs
            parseSharedExamplesInternal (examples @ [ { Tags = tags; Table = table; LineNumber = lineNumber }]) lines
        | _ -> examples, origLines

    parseSharedExamplesInternal [] lines

let private parseItem lines =
    let parseBulletPoints lines =
        let rec parseBulletPointsInternal bullets lines =
            match lines with
            | (_, _, Item(_, BulletPoint p)) :: xs -> parseBulletPointsInternal (bullets @ [ p ]) xs
            | _ -> bullets, lines
        parseBulletPointsInternal [] lines

    let parseMultiLineString lines =
        let offset, lines =
            match lines with
            | (_, _, Item(_, MultiLineStringStart o)) :: xs -> o, xs
            | _ -> lines |> raiseParseException "DocString start expected"

        let rec readLines (sb:StringBuilder) offset lines =
            match lines with
            | (_, _, Item(_, MultiLineString s)) :: xs ->
                if sb.Length > 0 then sb.AppendLine("") |> ignore

                if s.Length > offset then
                    sb.Append(s.Substring(offset)) |> ignore

                readLines sb offset xs
            | _ -> sb.ToString(), lines

        let text, lines = readLines (new StringBuilder()) offset lines

        match lines with
        | (_, _, Item(_, MultiLineStringEnd)) :: xs -> text, xs
        | _ -> lines |> raiseParseException "DocString end expected"

    match lines with
    | (_, _, Item(_, BulletPoint _)) :: _ ->
        let bullets, lines = parseBulletPoints lines
        Some (BulletsItem bullets), lines
    | (_, _, Item(_, MultiLineStringStart _)) :: _ ->
        let string, lines = parseMultiLineString lines
        Some (DocStringItem string), lines
    | (_, _, Item(_, TableRow _)) :: _ ->
        let table, lines = parseTable lines
        Some (TableItem table), lines
    | _ -> None, lines

let private parseSteps lines =
    let parseStep lines =
        match lines with
        | (ln,line,Step(s)) :: xs ->
            let item, newLines = parseItem xs
            Some {
                Step = s
                LineNumber = ln
                LineString = line
                Item = item
            }, newLines
        | _ -> None, lines

    let rec parseStepsInternal steps lines =
        let parsedStep, lines = parseStep lines
        match parsedStep with
        | Some step -> parseStepsInternal (step :: steps) lines
        | None -> steps, lines

    let steps, lines = parseStepsInternal [] lines
    if steps = [] then lines |> raiseParseException "At least one step is expected"
    steps |> List.rev, lines

let private parseBackground lines =
    match lines with
    | (_,_,Background) :: xs -> parseSteps xs
    | _ -> [], lines

let private parseScenario origLines =
    let tags, lines = parseTags origLines

    let scenarioName, lines =
        match lines with
        | (_,_,Scenario name) :: xs -> Some name, xs
        | _ -> None, lines

    match scenarioName with
    | Some name ->
        let steps, lines = parseSteps lines
        let examples, lines = parseExamples lines

        Some {
            Name = name
            Tags = tags
            Steps = steps
            Examples = examples
        }, lines
    | None -> None, origLines

let private parseScenarios lines =
    let rec parseScenariosInternal scenarios lines =
        let parsedScenario, lines = parseScenario lines
        match parsedScenario with
        | Some scenario -> parseScenariosInternal (scenario :: scenarios) lines
        | None -> scenarios, lines

    let scenarios, lines = parseScenariosInternal [] lines
    if scenarios = [] then lines |> raiseParseException "At least one scenario is expected"
    scenarios |> List.rev, lines

let private parseFeatureBlock lines =
    let tags, lines = parseTags lines

    let featureName, lines =
        match lines with
        | (_,_,FeatureName name) :: xs -> name, xs
        | _ -> lines |> raiseParseException "Expected feature in the beginning of file"

    let rec skipDescription lines =
        match lines with
        | (_,_,FeatureDescription _) :: xs -> skipDescription xs
        | x -> x

    let lines = skipDescription lines
    let background, lines = parseBackground lines
    let scenarios, lines = parseScenarios lines
    let examples, lines = parseSharedExamples lines

    if lines <> [] then lines |> raiseParseException "File continues unexpectedly"

    {
        Name = featureName
        Tags = tags
        Background = background
        Scenarios = scenarios
        SharedExamples = examples
    }

let private parseFeatureFile parsedLines =
    match parsedLines with
    | (_,_,FileStart) :: xs -> parseFeatureBlock xs
    | _ -> parsedLines |> raiseParseException "Unexpected call of parser"

type internal ScannedLine =
    // Scanned line: (lineNumber, lineContent, lineType) of parsed line
    | Line of int * string * LineType
    // Ignored line: lineType of last preceeding parsed non-ignored line
    | IgnoredLine of LineType

let parseBlocks (lines:string seq) =
    lines
    |> Seq.mapi (fun lineNumber line -> (lineNumber + 1, line))
    |> Seq.map (fun (lineNumber, line) ->
        let i = line.IndexOf("#")
        if i = -1 then lineNumber, line
        else lineNumber, line.Substring(0, i)
    )
    |> Seq.scan(fun prevLine (lineNumber, lineContent) ->
        let lastParsedLine =
            match prevLine with
            | Line (_, _, prevLineType) -> prevLineType
            | IgnoredLine prevLineType -> prevLineType
        let parsed = parseLine (lastParsedLine, lineContent)
        match parsed with
        | Some line -> (lineNumber, lineContent, line) |> Line
        | None when lineContent.Trim().Length = 0 ->
            lastParsedLine |> IgnoredLine
        | None ->
            let e = expectingLine lastParsedLine
            let m = sprintf "Syntax error on line %d %s\r\n%s" lineNumber lineContent e
            ParseException(m, Some lineNumber) |> raise
        ) (Line (0, "", FileStart))
    |> Seq.choose (function
        | IgnoredLine _ -> None
        | Line (lineNumber, lineContent, lineType) -> Some (lineNumber, lineContent, lineType)
    )
    |> Seq.toList
    |> parseFeatureFile
