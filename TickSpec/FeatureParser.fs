module TickSpec.FeatureParser

open System.Text.RegularExpressions
open TickSpec.BlockParser
open System

/// Parses lines of feature
let parseFeature (lines:string[]) =
    let replace combination s =
        let lookup (m:Match) =
            let x = m.Value.TrimStart([|'<'|]).TrimEnd([|'>'|])
            combination |> Seq.tryFind (fun (k,_) -> k = x)
            |> (function Some(_,v) -> v | None -> m.Value)
        let pattern = "<([^<]*)>"
        Regex.Replace(s, pattern, lookup)

    let computeCombinations examples =
        let rec combinations source =
            match source with
            | [] -> [[]]
            | (header, rows) :: xs ->
                [ for row in rows do
                    for combinedRow in combinations xs ->
                        (header, row) :: combinedRow ]

        let processRow (rowSet:(string list * (Map<string,string> * string list)) list) : (string list * Map<string,string>) option = None

        examples
        |> Seq.map (fun exampleBlock ->
            exampleBlock.Table.Header |> List.sort,
            exampleBlock.Table.Rows
            |> Seq.map (fun row ->
                Seq.zip exampleBlock.Table.Header row
                |> Seq.fold (fun map (header, value) ->
                        match Map.tryFind header map with
                        | Some x -> Exception("Multiple values for a single header") |> raise
                        | None -> map |> Map.add header value
                    ) Map.empty,
                exampleBlock.Tags
            )
        )
        // Union tables with the same columns
        |> Seq.groupBy (fun (h,_) -> h)
        |> Seq.map (fun (header,tables) ->
            header, tables |> Seq.collect (fun (_, rows) -> rows) |> Seq.toList)
        |> Seq.toList
        // Cross-join tables with different columns
        |> combinations
        |> Seq.map processRow
        |> Seq.choose id
        |> Seq.groupBy (fun (_,r) -> r)
        |> Seq.map (fun (row, taggedRows) ->
            taggedRows
            |> Seq.fold (fun tags (t,r) ->
                Seq.append tags t
            ) Seq.empty
            |> Seq.distinct
            |> Seq.toList,
            row |> Map.toList
        )


        // [(["Tag"], [("Lorem", "Ipsum")])]

    let createStep combination step =
        let processedStep =
            match step.Step with
            | GivenStep s -> replace combination s |> GivenStep
            | WhenStep s -> replace combination s |> WhenStep
            | ThenStep s  -> replace combination s |> ThenStep

        let bullets, table, doc =
            match step.Item with
            | Some (BulletsItem b) -> Some (b |> List.toArray), None, None
            | Some (TableItem t) -> None, Some (new Table(t.Header |> List.toArray, t.Rows |> Seq.map List.toArray |> Seq.toArray)), None
            | Some (DocStringItem d) -> None, None, Some d
            | None -> None, None, None

        (processedStep, { Number = step.LineNumber; Text=step.LineString; Bullets = bullets; Table = table; Doc = doc})

    let parsedFeatureBlocks = parseBlocks lines
    let sharedExamples = parsedFeatureBlocks.SharedExamples
    let background = parsedFeatureBlocks.Background

    let scenarios =
        parsedFeatureBlocks.Scenarios
        |> Seq.collect (fun scenario ->
            let examples = scenario.Examples @ sharedExamples

            let exampleCombinations = computeCombinations examples
            exampleCombinations
            |> Seq.mapi (fun i (tags, combination) ->
                let name = sprintf "%s (%d)" scenario.Name i
                let steps =
                    background @ scenario.Steps
                    |> Seq.map (createStep combination)
                    |> Seq.toArray

                { Name=name; Tags=tags |> List.toArray; Steps=steps; Parameters=combination |> List.toArray }
            )
        )

    { Name = parsedFeatureBlocks.Name; Scenarios = scenarios |> Seq.toArray }