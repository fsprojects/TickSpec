module internal TickSpec.Parser

open TickSpec.LineParser

let identity x = x

let mapItems items = 
    let items = List.rev items
    match items with
    | x::xs ->
        match x with
        | BulletPoint _ ->
            let bullets =
                items |> List.map (function
                    | TableRow _ -> None
                    | BulletPoint s -> Some s
                )
                |> List.choose identity
            Some(bullets |> List.toArray),None
        | TableRow header ->
            let rows =
                xs |> List.map (function
                    | TableRow cols -> Some cols
                    | BulletPoint _ -> None
                )
                |> List.choose identity
            None,Some(Table(header,rows |> List.toArray))
    | [] -> None,None

/// Build scenarios in specified lines
let buildScenarios lines =
    // Scan over lines
    lines
    |> Seq.scan (fun (scenario,scenarioN,lastStep,lastN,tags,tags',_) (lineN,line) ->
        let step = 
            match parseLine (lastStep,line) with
            | Some newStep -> newStep
            | None -> 
                let e = expectingLine lastStep
                let m = sprintf "Syntax error on line %d %s\r\n%s" lineN line e
                StepException(m,lineN,scenario.ToString()) |> raise
        match step with
        | Tag tag ->
            scenario, scenarioN, step, lineN, tag::tags, tags', None
        | ScenarioStart scenario -> 
            scenario, scenarioN+1, step, lineN, [], tags, None
        | ExamplesStart | GivenStep _ | WhenStep _ | ThenStep _ ->
            scenario, scenarioN, step, lineN, tags, tags', 
                Some(scenario,scenarioN,tags',lineN,line,step) 
        | Item _ ->
            scenario, scenarioN, step, lastN, tags, tags', 
                Some(scenario,scenarioN,tags',lastN,line,step)
    ) (Background,0,ScenarioStart(Background),0,[],[],None)
    // Handle tables
    |> Seq.choose (fun (_,_,_,_,_,_,step) -> step)
    |> Seq.groupBy (fun (_,_,_,lineN,_,_) -> lineN)    
    |> Seq.map (fun (line,items) ->
        items |> Seq.fold (fun (row,table) (scenario,scenarioN,tags,lineN,line,step) ->
            match step with
            | ScenarioStart Shared
            | ExamplesStart | GivenStep _ | WhenStep _ | ThenStep _ ->
                (scenario,scenarioN,tags,lineN,line,step),table
            | Item (ScenarioStart Shared,item) ->
                (scenario,scenarioN,tags,lineN,line,step), item::table
            | Item (_,item) ->
                row, item::table
            | ScenarioStart _ | Tag _ -> 
                invalidOp("")
        ) ((Background,0,[],0,"",ScenarioStart(Background)),[])
        |> (fun (line, items) -> line, mapItems items)
    )
    // Map to lines
    |> Seq.map (fun ((scenario,scenarioN,tags,n,line,step),(bullets,table)) ->
        let line = {Number=n;Text=line;Bullets=bullets;Table=table}
        scenario,scenarioN,tags,line,step
    )
    // Group into scenarios
    |> Seq.groupBy (fun (scenario,n,tags,_,_) -> (scenario,n,tags))
    |> (fun (scenarios) ->
        let names = scenarios |> Seq.map (fun ((name,_,_),_) -> name)
        scenarios |> Seq.mapi (fun i ((name,_,tags),lines) ->
            let names = names |> Seq.take (i+1)
            let count = names |> Seq.filter ((=) name) |> Seq.length
            let name = 
                if count = 1 then name 
                else
                    match name with
                    | Background ->
                        let message = "Multiple Backgrounds not supported"
                        raise (new System.NotSupportedException(message))
                    | Named text -> Named (sprintf "%s~%d" text count)
                    | Shared -> Shared
            (name,tags),lines
        )
    )
    |> Seq.map (fun (scenario,lines) -> 
        scenario,
            lines 
            |> Seq.toArray
            |> Array.partition (function
                | _,_,_,_,ExamplesStart -> true 
                | _ -> false
            )
            |> (fun (examples,steps) ->
                steps, 
                    let tables =
                        examples      
                        |> Array.choose (fun (_,_,_,line,_) -> line.Table)
                        |> Array.filter (fun table -> table.Rows.Length > 0)
                    if tables.Length > 0 then Some tables
                    else None
            )
    )
    |> Seq.map (fun ((scenario,tags),(steps,examples)) -> 
        scenario,tags |> List.toArray,steps,examples
    )

/// Parse feature lines
let parse (featureLines:string[]) =
    let startsWith s (line:string) = line.Trim().StartsWith(s)
    let lines =
        featureLines
        |> Seq.mapi (fun i line -> (i+1,line))
    let n, feature =
        lines
        |> Seq.tryFind (snd >> startsWith "Feature")
        |> (function Some line -> line | None -> invalidOp(""))
    let scenarios =
        lines
        |> Seq.skip n
        |> Seq.skipUntil (fun (_,text) ->
            text |> startsWith "@" ||
            text |> startsWith "Scenario" || 
            text |> startsWith "Story" ||
            text |> startsWith "Background" ||
            text |> startsWith "Shared"
        )
        |> Seq.map (fun (n,line) -> 
            let i = line.IndexOf("#")
            if i = -1 then n,line
            else n,line.Substring(0,i)
        )
        |> Seq.filter (fun (_,line) -> line.Trim().Length > 0)
        |> buildScenarios
    let background =
        scenarios 
        |> Seq.choose (function 
            | Background,tags,lines,examples -> Some (lines,examples)
            | Named _,_,_,_ -> None
            | Shared,_,_,_ -> None
        ) 
        |> Seq.collect (fun (lines,_) -> lines)
    let sharedExamples =
        scenarios
        |> Seq.choose (function 
            | Background,_,_,_ -> None
            | Named _,_,_,_ -> None
            | Shared,tags,lines,examples -> 
                let tables = lines |> Seq.map (fun (_,_,_,line,_) -> line.Table) |> Seq.choose identity
                examples |> (function Some x -> Seq.append tables x | None -> tables) 
                |> Some
        )
        |> Seq.concat |> Seq.toArray
    let scenarios =
        scenarios
        |> Seq.choose (function 
            | Background,_,_,_ -> None
            | Named name,tags,lines,examples -> Some(name,tags,lines,examples)
            | Shared,_,_,_ -> None
        )
    feature, background, scenarios, sharedExamples
