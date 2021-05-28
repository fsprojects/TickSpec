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

        let processRow rowSet =
            rowSet
            |> List.fold (fun state (_, (rowMap, rowTags)) ->
                match state with
                | None -> None
                | Some (stateTags, stateMap) ->
                    let newStateMap =
                        (Some stateMap, rowMap)
                        ||> Map.fold (fun state key value ->
                            match state with
                            | None -> None
                            | Some map ->
                                let existingValue = map |> Map.tryFind key
                                match existingValue with
                                | None -> Some (map.Add (key, value))
                                | Some x when x = value -> Some map
                                | _ -> None)
                    match newStateMap with
                    | None -> None
                    | Some s ->
                        let newStateTags = stateTags @ rowTags
                        Some (newStateTags, s)
            ) (Some (List.Empty, Map.empty))

        examples
        |> Seq.map (fun exampleBlock ->
            let headers = exampleBlock.Table.Header |> List.sort
            let rows =
                exampleBlock.Table.Rows
                |> Seq.map (fun row ->
                    let rowMap =
                        (Map.empty, Seq.zip exampleBlock.Table.Header row)
                        ||> Seq.fold (fun map (header, value) ->
                                match Map.tryFind header map with
                                | Some x ->
                                    let m = sprintf "A single header was specified multiple times in an example block starting at row %d" exampleBlock.LineNumber
                                    ParseException(m, Some exampleBlock.LineNumber) |> raise
                                | None -> map |> Map.add header value)
                    rowMap, exampleBlock.Tags)
            headers, rows)
        // Union tables with the same columns
        |> Seq.groupBy (fun (h,_) -> h)
        |> Seq.map (fun (header,tables) ->
            header, tables |> Seq.collect (fun (_, rows) -> rows) |> Seq.toList)
        |> Seq.toList
        // Cross-join tables with different columns
        |> combinations
        |> Seq.choose processRow
        |> Seq.groupBy (fun (_,r) -> r)
        |> Seq.map (fun (row, taggedRows) ->
            let t =
                (Seq.empty, taggedRows)
                ||> Seq.fold (fun tags (t,_) -> Seq.append tags t)
                |> Seq.distinct
                |> Seq.toList
            t, row |> Map.toList
        )

    let createStep combination step =
        let processedStep =
            match step.Step with
            | GivenStep s -> replace combination s |> GivenStep
            | WhenStep s -> replace combination s |> WhenStep
            | ThenStep s  -> replace combination s |> ThenStep

        let bullets, table, doc =
            match step.Item with
            | Some (BulletsItem b) ->
                b |> List.toArray |> Some, None, None
            | Some (TableItem t) ->
                let header = t.Header |> List.toArray
                let rows = 
                    t.Rows 
                    |> Seq.map (List.map (fun s -> replace combination s))
                    |> Seq.map List.toArray 
                    |> Seq.toArray
                None, new Table(header, rows) |> Some, None
            | Some (DocStringItem d) -> None, None, Some d
            | None -> None, None, None

        (processedStep, { Number = step.LineNumber; Text=step.LineString; Bullets = bullets; Table = table; Doc = doc})

    let parsedFeatureBlocks = parseBlocks lines
    let sharedExamples = parsedFeatureBlocks.SharedExamples
    let background = parsedFeatureBlocks.Background

    let scenarios =
        parsedFeatureBlocks.Scenarios
        |> Seq.groupBy (fun s -> s.Name)
        |> Seq.collect (fun (_,scenarios) ->
            if scenarios |> Seq.isLengthExactly 1 then
                scenarios
            else
                scenarios |> Seq.mapi (fun i s ->
                    let newName = sprintf "%s~%d" s.Name (i+1)
                    { s with Name = newName }))
        |> Seq.collect (fun scenario ->
            let examples = scenario.Examples @ sharedExamples
            let baseTags = parsedFeatureBlocks.Tags @ scenario.Tags

            let exampleCombinations = computeCombinations examples |> Seq.toList
            let nameFunc =
                if exampleCombinations.Length > 1 then
                    fun i -> sprintf "%s (%d)" scenario.Name (i+1)
                else
                    fun _ -> scenario.Name

            exampleCombinations
            |> List.mapi (fun i (tags, combination) ->
                let name = nameFunc i
                let steps =
                    Seq.append background scenario.Steps
                    |> Seq.map (createStep combination)
                    |> Seq.toArray

                { Name=name; Tags=baseTags @ tags |> Seq.distinct |> Seq.toArray; Steps=steps; Parameters=combination |> List.toArray }
            )
        )

    { Name = parsedFeatureBlocks.Name; Scenarios = scenarios |> Seq.toArray }