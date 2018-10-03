module TickSpec.FeatureParser

open System.Text.RegularExpressions
open TickSpec.BlockParser

/// Computes combinations of table values
let internal computeCombinations (tables:Table []) =
    let values =
        tables
        |> Seq.map (fun table ->
            table.Rows |> Array.map (fun row ->
                row
                |> Array.mapi (fun i col ->
                    table.Header.[i],col
                )
            )
        )
        |> Seq.toList
    values |> List.combinations

/// Replace line with specified named values
let internal replaceLine (xs:seq<string * string>) (scenario,n,tags,line,step) =
    let replace s =
        let lookup (m:Match) =
            let x = m.Value.TrimStart([|'<'|]).TrimEnd([|'>'|])
            xs |> Seq.tryFind (fun (k,_) -> k = x)
            |> (function Some(_,v) -> v | None -> m.Value)
        let pattern = "<([^<]*)>"
        Regex.Replace(s, pattern, lookup)
    let step =
        match step with
        | GivenStep s -> replace s |> GivenStep
        | WhenStep s -> replace s |> WhenStep
        | ThenStep s  -> replace s |> ThenStep
    let table =
        line.Table
        |> Option.map (fun table ->
            Table(table.Header,
                table.Rows |> Array.map (fun row ->
                    row |> Array.map (fun col -> replace col)
                )
            )
        )
    let bullets =
        line.Bullets
        |> Option.map (fun bullets -> bullets |> Array.map replace)
    (scenario,n,tags,{line with Table=table;Bullets=bullets},step)

/// Appends shared examples to scenarios as examples
let internal appendSharedExamples (sharedExamples:Table[]) scenarios  =
    if Seq.length sharedExamples = 0 then
        scenarios
    else
        scenarios |> Seq.map (function
            | scenarioName,tags,steps,None ->
                scenarioName,tags,steps,Some(sharedExamples)
            | scenarioName,tags,steps,Some(exampleTables) ->
                scenarioName,tags,steps,Some(Array.append exampleTables sharedExamples)
        )

/// Parses lines of feature
let parseFeature (lines:string[]) =
    let computeCombinations examples = [(["Tag"], [("Lorem", "Ipsum")])]
    let updateStep combination step = { Step = GivenStep "Lorem"; LineNumber = 1; LineString = "Lorem"; Item = None }
    let convertToFeatureStep step = (GivenStep "Lorem", { Number = 1; Text="Lorem"; Bullets = None; Table = None; Doc = None})

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
                    |> Seq.map (updateStep combination)
                    |> Seq.map convertToFeatureStep
                    |> Seq.toArray

                { Name=name; Tags=tags |> List.toArray; Steps=steps; Parameters=combination |> List.toArray }
            )
        )

    { Name = parsedFeatureBlocks.Name; Scenarios = scenarios |> Seq.toArray }