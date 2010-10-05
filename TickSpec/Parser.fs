module internal TickSpec.Parser

open TickSpec.LineParser

/// Build scenarios in specified lines
let buildScenarios lines =
    // Scan over lines
    lines
    |> Seq.scan (fun (scenario,lastStep,lastN,tags,tags',_) (n,line) ->
        let step = 
            match parseLine (lastStep,line) with
            | Some newStep -> newStep
            | None -> 
                let e = expectingLine lastStep
                let m = sprintf "Syntax error on line %d %s\r\n%s" n line e
                StepException(m,n,scenario.ToString()) |> raise
        match step with
        | Tag tag ->
            scenario, step, n, tag::tags, tags', None
        | ScenarioStart scenario -> 
            scenario, step, n, [], tags, None
        | ExamplesStart          
        | GivenStep _ | WhenStep _ | ThenStep _ ->
            scenario, step, n, tags, tags', Some(scenario,tags',n,line,step) 
        | Item _ ->
            scenario, step, lastN, tags, tags', Some(scenario,tags',lastN,line,step)
    ) (Background,ScenarioStart(Background),0,[],[],None)
    // Handle tables
    |> Seq.choose (fun (_,_,_,_,_,step) -> step)
    |> Seq.groupBy (fun (_,_,n,_,_) -> n)
    |> Seq.map (fun (line,items) ->
        items |> Seq.fold (fun (row,table) (scenario,tags,n,line,step) ->
            match step with
            | ScenarioStart _ | Tag _ -> 
                invalidOp("")
            | ExamplesStart
            | GivenStep _ | WhenStep _ | ThenStep _ ->
                (scenario,tags,n,line,step),table
            | Item (_,item) ->
                row, item::table
        ) ((Background,[],0,"",ScenarioStart(Background)),[])
        |> (fun (line, items) -> 
            let items = List.rev items            
            line,
                match items with
                | x::xs ->
                    match x with
                    | BulletPoint _ ->
                        let bullets =
                           items |> List.map (function
                                | TableRow _ -> None
                                | BulletPoint s -> Some s
                            )
                            |> List.choose (fun x -> x)
                        Some(bullets |> List.toArray),None
                    | TableRow header ->
                        let rows =
                            xs |> List.map (function
                                | TableRow cols -> Some cols
                                | BulletPoint _ -> None
                            )
                            |> List.choose (fun x -> x)
                        None,Some(Table(header,rows |> List.toArray))
                | [] -> None,None                
        )
    )           
    // Map to lines
    |> Seq.map (fun ((scenario,tags,n,line,step),(bullets,table)) ->
        let line = {Number=n;Text=line;Bullets=bullets;Table=table}
        scenario,tags,line,step
    )
    // Group into scenarios
    |> Seq.groupBy (fun (scenario,tags,_,_) -> (scenario,tags))        
    // Handle examples
    |> Seq.map (fun (scenario,lines) -> 
        scenario,
            lines 
            |> Seq.toArray
            |> Array.partition (function 
                | _,_,_,ExamplesStart -> true 
                | _ -> false
            )
            |> (fun (examples,steps) ->
                steps, 
                    let tables =
                        examples      
                        |> Array.choose (fun (_,_,line,_) -> line.Table)
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
            text |> startsWith "Background"
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
            | (Background,tags,lines,examples) -> Some (lines,examples)
            | (Named _,_,_,_) -> None
        ) 
        |> Seq.collect (fun (lines,_) -> lines)                 
    let scenarios =
        scenarios
        |> Seq.choose (function 
            | (Background,_,_,_) -> None
            | (Named name,tags,lines,examples) -> Some(name,tags,lines,examples)
        )            
    feature, background, scenarios
