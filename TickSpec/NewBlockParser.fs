module internal TickSpec.NewBlockParser

open TickSpec.NewLineParser
open System

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
    }

let private parseTags lines =
    let rec parseTagsInternal tags lines =
        match lines with
        | (_,_,TagLine strings) :: xs -> parseTagsInternal (tags @ strings) xs
        | _ -> tags, lines
    parseTagsInternal [] lines

let private parseExamples lines =
    [], lines

let private parseSharedExamples lines =
    [], lines

let private parseItem lines =
    None, lines

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
    if steps = [] then Exception("At least one step is expected") |> raise
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
    if scenarios = [] then Exception("At least one scenario is expected") |> raise
    scenarios |> List.rev, lines

let private parseFeatureBlock lines =
    let tags, lines = parseTags lines

    let featureName, lines =
        match lines with
        | (_,_,FeatureName name) :: xs -> name, xs
        | _ -> Exception("Expected feature in the beginning of file") |> raise

    let rec skipDescription lines =
        match lines with
        | (_,_,FeatureDescription _) :: xs -> skipDescription xs
        | x -> x

    let lines = skipDescription lines
    let background, lines = parseBackground lines
    let scenarios, lines = parseScenarios lines
    let examples, lines = parseSharedExamples lines

    if lines <> [] then Exception("File continues unexpectedly") |> raise

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
    | _ -> Exception("Unexpected call of parser") |> raise

let parseBlocks (lines:string seq) =
    lines
    |> Seq.mapi (fun lineNumber line -> (lineNumber + 1, line))
    |> Seq.map (fun (lineNumber, line) ->
        let i = line.IndexOf("#")
        if i = -1 then lineNumber, line
        else lineNumber, line.Substring(0, i)
    )
    |> Seq.filter (fun (_, line) -> line.Trim().Length > 0)
    |> Seq.scan(fun (_, _, lastParsedLine) (lineNumber, lineContent) ->
        let parsed = parseLine (lastParsedLine, lineContent)
        match parsed with
        | Some line -> (lineNumber, lineContent, line)
        | None ->
            let e = expectingLine lastParsedLine
            let m = sprintf "Syntax error on line %d %s\r\n%s" lineNumber lineContent e
            Exception(m) |> raise
        ) (0, "", FileStart)
    |> Seq.toList
    |> parseFeatureFile

