namespace TickSpec

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions

/// Line type
type internal LineType = ScenarioStart | GivenStep | WhenStep | ThenStep

type Scenario = { Name:string; Action:Action }

/// Encapsulates step definitions for execution against features
type StepDefinitions (methods:MethodInfo seq) =            
    /// Returns method's step attribute or null
    static let GetStepAttribute (m:MemberInfo) = 
        Attribute.GetCustomAttribute(m,typeof<StepAttribute>)        
    /// Step methods
    let givens, whens, thens =
        methods |> Seq.fold (fun (gs,ws,ts) m ->            
            match GetStepAttribute m with        
            | :? GivenAttribute -> (m::gs,ws,ts)
            | :? WhenAttribute -> (gs,m::ws,ts)             
            | :? ThenAttribute -> (gs,ws,m::ts)
            | _ -> invalidOp("")
        ) ([],[],[])    
    /// Chooses matching definitions for specifed text
    let chooseDefinitions text definitions =        
        definitions |> List.choose (fun (m:MethodInfo) ->
            let a = GetStepAttribute m :?> StepAttribute           
            let pattern = if null = a.Step then m.Name else a.Step
            let r = Regex.Match(text,pattern)             
            if r.Success then Some(r,m) else None 
        )
    /// Chooses defininitons for specified step and text
    let matchStep text = function       
        | ScenarioStart -> invalidOp("")
        | GivenStep -> chooseDefinitions text givens
        | WhenStep -> chooseDefinitions text whens
        | ThenStep -> chooseDefinitions text thens
    /// Try single parameter regular expression
    let tryRegex input pattern =
        let m = Regex.Match(input, pattern)
        if m.Success then m.Groups.[1].Value |> Some
        else None

    let (|Scenario|_|) s = 
        tryRegex s "Scenario(.*)" 
        |> Option.map (fun t -> Scenario t)
    let (|Given|_|) s = 
        tryRegex s "Given\s+(.*)" 
        |> Option.map (fun t -> Given t)
    let (|When|_|) s = 
        tryRegex s "When\s+(.*)" 
        |> Option.map (fun t -> When t)
    let (|Then|_|) s = 
        tryRegex s "Then\s+(.*)" 
        |> Option.map (fun t -> Then t)
    let (|And|_|) s = 
        tryRegex s "And\s+(.*)" 
        |> Option.map (fun t -> And t)
    let (|But|_|) s = 
        tryRegex s "But\s+(.*)" 
        |> Option.map (fun t -> But t)    
   
    /// Line state given previous line state and new line text
    let matchLine = function 
        | _, Scenario(text) ->
            ScenarioStart, text
        | ScenarioStart, Given(text) | GivenStep, Given(text) 
        | GivenStep, And(text) | GivenStep, But(text) -> 
            GivenStep, text
        | ScenarioStart, When(text)
        | GivenStep, When(text) | WhenStep, When(text) 
        | WhenStep, And(text) | WhenStep, But(text) ->            
            WhenStep, text
        | ScenarioStart, Then(text) 
        | GivenStep, Then(text)
        | WhenStep, Then(text) | ThenStep, Then(text)
        | ThenStep, And(text) | ThenStep, But(text) -> 
            ThenStep,text        
        | _, line -> invalidOp(line)     
    /// Service provider
    let provider = 
        /// Type instances constructed for invoked steps
        let instances = Dictionary<_,_>() 
        /// Gets type instance for specified type
        let getInstance (t:Type) =        
            match instances.TryGetValue t with
            | true, instance -> instance
            | false, _ ->
                let cons = t.GetConstructor([||])
                let instance = cons.Invoke([||])
                instances.Add(t,instance)
                instance
        { new System.IServiceProvider with
            member this.GetService(t:Type) =
                getInstance t
        }
    /// Build argumens from match
    let buildArgs (r:Match) =        
        let args = List<string>()                                
        for i = 1 to r.Groups.Count-1 do            
            args.Add r.Groups.[i].Value
        args.ToArray()      
    /// Build scenarios in specified lines
    let buildScenarios lines =
        lines
        |> Seq.scan (fun (scenario,lastStep,_) (n,line) ->               
            let step, text = matchLine (lastStep,line)                
            System.Diagnostics.Debug.WriteLine text     
            match step with
            | ScenarioStart -> text, step, None
            | GivenStep | WhenStep | ThenStep ->
                let matches = matchStep text step           
                let fail e = StepException(e,n,scenario) |> raise
                if matches.IsEmpty then fail "Missing step"                     
                if matches.Length > 1 then fail "Ambiguous step"                                    
                let r,m = matches.Head 
                if m.GetParameters().Length <> r.Groups.Count-1 then
                    fail "Parameter count mismatch"                                   
                scenario, step, Some(scenario,n,line,m,buildArgs r)                     
        ) ("",ScenarioStart,None)
        |> Seq.choose (fun (_,_,step) -> step)      
        |> Seq.groupBy (fun (scenario,_,_,_,_) -> scenario)                  
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
            |> buildScenarios
        feature, scenarios
    /// Constructs instance by reflecting against specified types
    new (types:Type[]) =
        let methods = 
            types 
            |> Seq.collect (fun t -> t.GetMethods())       
            |> Seq.filter (fun m -> null <> GetStepAttribute m)
        StepDefinitions(methods)
    /// Constructs instance by reflecting against specified assembly
    new (assembly:Assembly) =
        StepDefinitions(assembly.GetTypes())
    /// Execute step definitions in specified lines
    member this.Execute (lines:string[]) =
        let featureName,scenarios = parse lines
        scenarios |> Seq.iter (fun scenario ->
            TickSpec.ScenarioRun.execute provider scenario
        )
    /// Generate scenario actions
    member this.GenerateScenarios (sourceUrl:string,lines:string[]) =        
        let featureName,scenarios = parse lines
        let gen = FeatureGen(featureName,sourceUrl)  
        scenarios |> Seq.map (fun (scenarioName,lines) ->
            let instance = 
                gen.GenScenario provider (scenarioName, Seq.toArray lines)
            let mi = instance.GetType().GetMethod("Run") 
            let action =
                 System.Action(fun () -> mi.Invoke(instance,null) |> ignore)             
            {Name=scenarioName; Action=action}                           
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

        