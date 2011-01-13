module internal TickSpec.BlockParser

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

/// Build blocks in specified lines
let buildBlocks lines =
    // Scan over lines
    lines
    |> Seq.scan (fun (block,blockN,lastStep,lastN,tags,tags',_) (lineN,line) ->
        let step = 
            match parseLine (lastStep,line) with
            | Some newStep -> newStep
            | None -> 
                let e = expectingLine lastStep
                let m = sprintf "Syntax error on line %d %s\r\n%s" lineN line e
                StepException(m,lineN,block.ToString()) |> raise
        match step with
        | TagLine tag ->
            block, blockN, step, lineN, tag@tags, tags', None
        | BlockStart block -> 
            block, blockN+1, step, lineN, [], tags, None
        | ExamplesStart | Step _ ->
            block, blockN, step, lineN, tags, tags', 
                Some(block,blockN,tags',lineN,line,step) 
        | Item _ ->
            block, blockN, step, lastN, tags, tags', 
                Some(block,blockN,tags',lastN,line,step)
    ) (Background,0,BlockStart(Background),0,[],[],None)
    // Handle tables
    |> Seq.choose (fun (_,_,_,_,_,_,step) -> step)
    |> Seq.groupBy (fun (_,_,_,lineN,_,_) -> lineN)    
    |> Seq.map (fun (line,items) ->
        items |> Seq.fold (fun (text,row,table) (block,blockN,tags,lineN,line,step) ->
            let text = if String.length text = 0 then line else text + "\r\n" + line            
            match step with
            | BlockStart (Shared _)
            | ExamplesStart | Step _ ->
                text, (block,blockN,tags,lineN,line,step), table
            | Item (BlockStart (Shared _),item) ->
                text, (block,blockN,tags,lineN,line,step), item::table
            | Item (_,item) ->
                text, row, item::table
            | BlockStart _ | TagLine _ -> 
                invalidOp "Unexpected token"
        ) ("",(Background,0,[],0,"",BlockStart(Background)),[])
        |> (fun (text, line, items) -> text, line, mapItems items)
    )
    // Map to lines
    |> Seq.map (fun (text, (block,blockN,tags,n,line,step), (bullets,table)) ->
        let line = {Number=n;Text=text;Bullets=bullets;Table=table}
        block,blockN,tags,line,step
    )
    // Group into blocks
    |> Seq.groupBy (fun (block,n,tags,_,_) -> (block,n,tags))
    |> (fun (blocks) ->
        let names = blocks |> Seq.map (fun ((name,_,_),_) -> name)
        blocks |> Seq.mapi (fun i ((name,_,tags),lines) ->
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
                    | Shared tag -> Shared tag
            (name,tags),lines
        )
    )
    // Handle examples
    |> Seq.map (fun (block,lines) -> 
        block,
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
    |> Seq.map (fun ((block,tags),(steps,examples)) -> 
        block,tags |> List.toArray,steps,examples
    )

/// Parse blocks
let parseBlocks (featureLines:string[]) =
    let startsWith s (line:string) = line.Trim().StartsWith(s)
    let lines =
        featureLines
        |> Seq.mapi (fun i line -> (i+1,line))
    let n, feature =
        lines
        |> Seq.tryFind (snd >> startsWith "Feature")
        |> (function Some line -> line | None -> invalidOp("Expecting Feature keyword"))
    let blocks =
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
        |> buildBlocks
    let tagExamples, sharedExamples =
        let shared =
            blocks
            |> Seq.choose (function
                | Background,_,_,_ -> None
                | Named _,_,_,_ -> None
                | Shared tag,tags,lines,examples -> 
                    let tables = lines |> Seq.map (fun (_,_,_,line,_) -> line.Table) |> Seq.choose identity
                    let examples = examples |> (function Some x -> Seq.append tables x | None -> tables)
                    (tag, examples) |> Some
            ) 
        let tagged = 
            shared |> Seq.choose (function Some x,y -> Some(x,y) | None,y -> None)
        let untagged = 
            shared |> Seq.choose (function Some x,y -> None | None,y -> Some y)
            |> Seq.concat |> Seq.toArray
        tagged, untagged
    let toSteps lines =
        lines 
        |> Seq.map (fun (block,n,tags,line,lineType) ->
            let step = match lineType with Step(step) -> step | _ -> invalidOp "Expecting step"
            (block,n,tags,line,step)
        )
    let background =
        blocks 
        |> Seq.choose (function 
            | Background,tags,lines,examples -> Some (lines,examples)
            | Named _,_,_,_ -> None
            | Shared _,_,_,_ -> None
        ) 
        |> Seq.collect (fun (lines,_) -> lines |> toSteps)
        |> Seq.toArray
    let scenarios =
        blocks
        |> Seq.choose (function 
            | Background,_,_,_ -> None
            | Named name,tags,lines,examples -> 
                let xs = tagExamples |> Seq.filter (fun (tag,_) -> tags |> Seq.exists ((=) tag)) |> Seq.collect snd |> Seq.toArray
                let examples =
                    match examples, xs with
                    | Some x, xs -> Array.append x xs |> Some
                    | None, xs -> if xs.Length>0 then Some xs else None
                Some(name,tags,lines |> toSteps,examples)
            | Shared _,_,_,_ -> None
        )
    feature, background, scenarios, sharedExamples