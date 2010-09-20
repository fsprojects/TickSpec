namespace TickSpec

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open TickSpec.LineParser
open TickSpec.ServiceProvider

type Scenario = { Name:string; Action:Action }

/// Encapsulates step definitions for execution against features
type StepDefinitions (methods:MethodInfo seq) =            
    /// Returns method's step attribute or null
    static let GetStepAttributes (m:MemberInfo) = 
        Attribute.GetCustomAttributes(m,typeof<StepAttribute>)                
    /// Step methods
    let givens, whens, thens =
        methods |> Seq.fold (fun (gs,ws,ts) m ->            
            match (GetStepAttributes m).[0] with        
            | :? GivenAttribute -> (m::gs,ws,ts)
            | :? WhenAttribute -> (gs,m::ws,ts)             
            | :? ThenAttribute -> (gs,ws,m::ts)
            | _ -> invalidOp("")
        ) ([],[],[])    
    /// Chooses matching definitions for specifed text
    let chooseDefinitions text definitions =  
        let chooseDefinition pattern =
            let r = Regex.Match(text,pattern)
            if r.Success then Some r else None        
        definitions |> List.choose (fun (m:MethodInfo) ->               
            let steps = 
                Attribute.GetCustomAttributes(m,typeof<StepAttribute>)
                |> Array.map (fun a -> (a :?> StepAttribute).Step)
                |> Array.filter ((<>) null)                                           
            match steps |> Array.tryPick chooseDefinition with            
            | Some r -> Some r
            | None -> chooseDefinition m.Name                
            |> Option.map (fun r -> r,m)
        )
    /// Chooses defininitons for specified step and text
    let matchStep = function       
        | ScenarioStart(_)
        | ExamplesStart | TableRow(_) -> 
            invalidOp("")
        | GivenStep(text) -> chooseDefinitions text givens
        | WhenStep(text) -> chooseDefinitions text whens
        | ThenStep(text) -> chooseDefinitions text thens
    /// Extract arguments from specified match
    let extractArgs (r:Match) =        
        let args = List<string>()
        for i = 1 to r.Groups.Count-1 do            
            r.Groups.[i].Value |> args.Add
        args.ToArray()      
    /// Build scenarios in specified lines
    let buildScenarios lines =
        lines
        |> Seq.scan (fun (scenario,lastStep,lastN,_) (n,line) ->               
            let step = parseLine (lastStep,line)                            
            match step with
            | ScenarioStart(name) ->
                name, step, n, None
            | ExamplesStart          
            | GivenStep(_) | WhenStep(_) | ThenStep(_) ->                                          
                scenario, step, n, Some(scenario,n,line,step) 
            | TableRow(_) ->
                scenario, step, lastN, Some(scenario,lastN,line,step)                                           
        ) ("",ScenarioStart(""),0,None)
        // Handle tables
        |> Seq.choose (fun (_,_,_,step) -> step)
        |> Seq.groupBy (fun (_,n,_,_) -> n)
        |> Seq.map (fun (line,items) ->
            items |> Seq.fold (fun (row,table) (scenario,n,line,step) ->
                match step with
                | ScenarioStart _ -> 
                    invalidOp("")
                | ExamplesStart
                | GivenStep _ | WhenStep _ | ThenStep _ ->
                    (scenario,n,line,step),table
                | TableRow columns ->
                    row,columns::table
            ) (("",0,"",ScenarioStart("")),[])
            |> (fun (line,table) -> 
                let table = List.rev table
                line,
                    match table with
                    | x::xs -> Some(Table(x,xs |> List.toArray))
                    | [] -> None                 
            )
        )           
        // Group into scenarios
        |> Seq.map (fun ((scenario,n,line,step),table) ->
            scenario,n,line,step,table
        )
        |> Seq.groupBy (fun (scenario,_,_,_,_) -> scenario)                     
        // Handle examples
        |> Seq.map (fun (scenario,lines) -> 
            scenario,
                lines 
                |> Seq.toArray
                |> Array.partition (function 
                    | _,_,_,ExamplesStart,_ -> true 
                    | _ -> false                    
                )                                 
                |> (fun (examples,steps) ->
                    steps, 
                        let tables =
                            examples      
                            |> Array.choose (fun (_,_,_,_,table) -> table)
                            |> Array.filter (fun table -> table.Rows.Length > 0)                                                                                                                        
                        if tables.Length > 0 then Some tables
                        else None                        
                )                                
        ) 
        |> Seq.map (fun (name,(steps,examples)) -> name,steps,examples)                        
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
            |> Seq.skipUntil (snd >> startsWith "Scenario")
            |> Seq.filter (fun (_,line) -> line.Trim().Length > 0)          
            |> Seq.filter (fun (_,line) -> not(line.Trim().StartsWith("#")))
            |> buildScenarios
        feature, scenarios
    /// Resolves line        
    let resolveLine (scenario,n,line,step,table) =
        let matches = matchStep step           
        let fail e = StepException(e,n,scenario) |> raise
        if matches.IsEmpty then fail "Missing step"                     
        if matches.Length > 1 then fail "Ambiguous step"                                    
        let r,m = matches.Head
        if m.ReturnType <> typeof<Void> then 
            fail "Step methods must return void/unit"
        let tableCount = table |> Option.count
        if m.GetParameters().Length <> (r.Groups.Count-1+tableCount) then
            fail "Parameter count mismatch"         
        scenario,n,line,m,extractArgs r,table
    /// Replace line with specified named values
    let replaceLine 
            (xs:seq<string * string>) 
            (scenario,n,line,step,table:Table option) =
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
            | _ -> invalidOp("")            
        let table =
            table 
            |> Option.map (fun table ->
                Table(table.Header,
                    table.Rows |> Array.map (fun row ->
                        row |> Array.map (fun col -> replace col)
                    )
                )
            )            
        (scenario,n,line,step,table)
    /// Computes combinations of table values
    let computeCombinations (tables:Table []) =
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
    /// Type instance provider    
    let provider = CreateServiceProvider ()
    /// Constructs instance by reflecting against specified types
    new (types:Type[]) =
        let methods = 
            types 
            |> Seq.collect (fun t -> t.GetMethods())       
            |> Seq.filter (fun m -> (GetStepAttributes m).Length > 0)
        StepDefinitions(methods)    
    /// Constructs instance by reflecting against specified assembly
    new (assembly:Assembly) =
        StepDefinitions(assembly.GetTypes())
    /// Execute step definitions in specified lines
    member this.Execute (lines:string[]) =
        let featureName,scenarios = parse lines        
        scenarios |> Seq.iter (function
            | name,lines,None ->
                let lines = lines |> Seq.map resolveLine
                TickSpec.ScenarioRun.execute provider (name,lines)
            | name,lines,Some(exampleTables) ->
                /// All combinations of tables
                let combinations = computeCombinations exampleTables                                                                        
                // Execute each combination
                combinations |> List.iter (fun combination ->
                    let combination = Seq.concat combination
                    let lines = 
                        lines
                        |> Seq.map (replaceLine combination)
                        |> Seq.map resolveLine
                    TickSpec.ScenarioRun.execute provider (name,lines)
                )
        )
    member this.Execute (reader:TextReader) =
        this.Execute(TextReader.readAllLines reader)           
    member this.Execute (feature:System.IO.Stream) =        
        use reader = new StreamReader(feature)
        this.Execute (reader)
    /// Generate scenario actions
    member this.GenerateScenarios (sourceUrl:string,lines:string[]) =        
        let featureName,scenarios = parse lines
        let gen = FeatureGen(featureName,sourceUrl)  
        let createAction (scenarioName, lines) =
            let instance = 
                gen.GenScenario provider (scenarioName, lines)
            let mi = instance.GetType().GetMethod("Run")             
            fun () -> mi.Invoke(instance,null) |> ignore                                
        scenarios |> Seq.map (function
            | scenarioName,lines,None ->
                let lines = lines |> Seq.map resolveLine |> Seq.toArray
                let action = createAction (scenarioName, lines)
                {Name=scenarioName; Action=System.Action(action)}
            | scenarioName,lines,Some(exampleTables) ->
                /// All combinations of tables
                let combinations = computeCombinations exampleTables                                                                        
                // Create action for each combination
                combinations |> List.mapi (fun i combination ->
                    let combination = Seq.concat combination
                    let lines = 
                        lines
                        |> Seq.map (replaceLine combination)
                        |> Seq.map resolveLine
                        |> Seq.toArray
                    createAction (sprintf "%s(%d)" scenarioName i,lines)                    
                )                                    
                |> (fun actions ->
                    let action = fun () -> 
                        actions |> Seq.iter (fun action -> action())
                    {Name=scenarioName; Action=System.Action(action)}
                )       
        )
    member this.GenerateScenarios (sourceUrl:string,reader:TextReader) =              
        this.GenerateScenarios(sourceUrl, TextReader.readAllLines reader)        
    member this.GenerateScenarios (sourceUrl:string,feature:System.IO.Stream) =        
        use reader = new StreamReader(feature)
        this.GenerateScenarios(sourceUrl, reader)
    member this.GenerateScenarios (path:string) =
        this.GenerateScenarios(path,File.ReadAllLines(path))
    /// Execute step definitions in specified lines from source document
    member this.Execute (sourceUrl:string,lines:string[]) =
        let scenarios = this.GenerateScenarios(sourceUrl,lines)
        scenarios |> Seq.iter (fun action -> action.Action.Invoke())                   
    member this.Execute (sourceUrl:string,reader:TextReader) =              
        this.Execute(sourceUrl, TextReader.readAllLines reader)           
    member this.Execute (sourceUrl:string,feature:System.IO.Stream) =        
        use reader = new StreamReader(feature)
        this.Execute (sourceUrl,reader)
    member this.Execute (path:string) =
        this.Execute(path,File.ReadAllLines(path))

        