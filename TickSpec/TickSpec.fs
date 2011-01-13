namespace TickSpec

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open TickSpec.LineParser
open TickSpec.FeatureParser
open TickSpec.ScenarioRun

/// Encapsulates Gherkin feature
type Feature = { 
    Name : string; 
    Source : string;
    Assembly : Assembly; 
    Scenarios : Scenario seq
    }

/// Encapsulates step definitions for execution against features
type StepDefinitions (givens,whens,thens,valueParsers) =
    /// Returns method's step attribute or null
    static let GetStepAttributes (m:MemberInfo) = 
        Attribute.GetCustomAttributes(m,typeof<StepAttribute>)
    /// Chooses matching definitions for specifed text
    let chooseDefinitions text definitions =  
        let chooseDefinition pattern =
            let r = Regex.Match(text,pattern)
            if r.Success then Some r else None
        definitions |> List.choose (fun (pattern:string,m:MethodInfo) ->
            chooseDefinition pattern |> Option.map (fun r -> r,m)
        )
    /// Chooses defininitons for specified step and text
    let matchStep = function
        | GivenStep text -> chooseDefinitions text givens
        | WhenStep text -> chooseDefinitions text whens
        | ThenStep text -> chooseDefinitions text thens
    /// Extract arguments from specified match
    let extractArgs (r:Match) =        
        let args = List<string>()
        for i = 1 to r.Groups.Count-1 do
            r.Groups.[i].Value |> args.Add
        args.ToArray()      
    /// Resolves line
    let resolveLine (scenario:ScenarioSource) (step,line) =
        let matches = matchStep step
        let fail e =
            let m = sprintf "%s on line %d" e line.Number
            StepException(m,line.Number,scenario.Name) |> raise
        if matches.IsEmpty then fail "Missing step definition"
        if matches.Length > 1 then fail "Ambiguous step definition"
        let r,m = matches.Head
        if m.ReturnType <> typeof<Void> then 
            fail "Step methods must return void/unit"
        let tableCount = line.Table |> Option.count
        let bulletsCount = line.Bullets |> Option.count
        let argCount = r.Groups.Count-1+tableCount+bulletsCount
        if m.GetParameters().Length <> argCount then
            fail "Parameter count mismatch"
        line,m,extractArgs r
    /// Gets description as scenario lines
    let getDescription steps =
            steps 
            |> Seq.map (fun (line,_,_) -> line.Text)
            |> String.concat "\r\n"
    new () =
        StepDefinitions(Assembly.GetCallingAssembly())
    /// Constructs instance by reflecting against specified assembly
    new (assembly:Assembly) =
        StepDefinitions(assembly.GetTypes())
    /// Constructs instance by reflecting against specified types
    new (types:Type[]) =
        let methods = 
            types 
            |> Seq.collect (fun t -> t.GetMethods())
        StepDefinitions(methods)
    new (methods:MethodInfo seq) =
        /// Step methods
        let givens, whens, thens =
            methods 
            |> Seq.map (fun m -> m, GetStepAttributes m)
            |> Seq.filter (fun (m,ca) -> ca.Length > 0)
            |> Seq.collect (fun (m,ca) -> ca |> Seq.map (fun a -> a,m))
            |> Seq.fold (fun (gs,ws,ts) (a,m) -> 
                let pattern = 
                    match (a :?> StepAttribute).Step with
                    | null -> m.Name
                    | step -> step
                match a with
                | :? GivenAttribute -> ((pattern,m)::gs,ws,ts)
                | :? WhenAttribute -> (gs,(pattern,m)::ws,ts)
                | :? ThenAttribute -> (gs,ws,(pattern,m)::ts)
                | _ -> invalidOp "Unhandled StepAttribute"
            ) ([],[],[])    
        /// Parser methods
        let valueParsers =
            methods 
            |> Seq.filter (fun m -> null <> Attribute.GetCustomAttribute(m,typeof<ParserAttribute>))
            |> Seq.map (fun m -> m.ReturnType, m)        
            |> Dict.ofSeq
        StepDefinitions(givens,whens,thens,valueParsers)
    /// Generate scenarios from specified lines (source undefined)
    member this.GenerateScenarios (lines:string []) =
        let featureSource = parseFeature lines
        featureSource.Scenarios
        |> Seq.map (fun scenario ->
            let steps = 
                scenario.Steps 
                |> Seq.map (resolveLine scenario)
                |> Seq.toArray
            let action = generate valueParsers (scenario.Name,steps)
            {Name=scenario.Name;Description=getDescription steps;
             Action=TickSpec.Action(action);Parameters=scenario.Parameters;Tags=scenario.Tags}
        )
    member this.GenerateScenarios (reader:TextReader) =
        this.GenerateScenarios(TextReader.readAllLines reader)
    member this.GenerateScenarios (feature:System.IO.Stream) =
        use reader = new StreamReader(feature)
        this.GenerateScenarios(reader)
    /// Execute step definitions in specified lines (source undefined)
    member this.Execute (lines:string[]) =
        this.GenerateScenarios lines
        |> Seq.iter (fun scenario -> scenario.Action.Invoke())
    member this.Execute (reader:TextReader) =
        this.Execute(TextReader.readAllLines reader)
    member this.Execute (feature:System.IO.Stream) =
        use reader = new StreamReader(feature)
        this.Execute (reader)
    /// Generates feature in specified lines from source document
    member this.GenerateFeature (sourceUrl:string,lines:string[]) =
        let featureSource = parseFeature lines
        let gen = FeatureGen(featureSource.Name,sourceUrl)
        let createAction (scenarioName, lines, ps) =
            let t = gen.GenScenario valueParsers (scenarioName, lines, ps)
            let instance = Activator.CreateInstance t
            let mi = instance.GetType().GetMethod("Run")
            TickSpec.Action(fun () -> mi.Invoke(instance,[||]) |> ignore)      
        let scenarios = 
            featureSource.Scenarios
            |> Seq.map (fun scenario ->
                let steps = 
                    scenario.Steps 
                    |> Seq.map (resolveLine scenario) 
                    |> Seq.toArray           
                let action = createAction (scenario.Name, steps, scenario.Parameters)           
                { Name=scenario.Name;Description=getDescription steps;
                      Action=action;Parameters=scenario.Parameters;Tags=scenario.Tags}
            )
        { Name = featureSource.Name; 
          Source = sourceUrl;
          Assembly = gen.Assembly; 
          Scenarios = scenarios |> Seq.toArray 
        }
    member this.GenerateFeature (sourceUrl:string,reader:TextReader) =
        this.GenerateFeature(sourceUrl, TextReader.readAllLines reader)
    member this.GenerateFeature (sourceUrl:string,feature:System.IO.Stream) =
        use reader = new StreamReader(feature)
        this.GenerateFeature(sourceUrl, reader)
#if SILVERLIGHT
#else   
    member this.GenerateFeature (path:string) =
        this.GenerateFeature(path,File.ReadAllLines(path))
#endif
    /// Generates scenarios in specified lines from source document
    member this.GenerateScenarios (sourceUrl:string,lines:string[]) =
        this.GenerateFeature(sourceUrl,lines).Scenarios    
    member this.GenerateScenarios (sourceUrl:string,reader:TextReader) =
        this.GenerateScenarios(sourceUrl, TextReader.readAllLines reader)
    member this.GenerateScenarios (sourceUrl:string,feature:System.IO.Stream) =
        use reader = new StreamReader(feature)
        this.GenerateScenarios(sourceUrl, reader)
#if SILVERLIGHT
#else 
    member this.GenerateScenarios (path:string) =
        this.GenerateScenarios(path,File.ReadAllLines(path))
#endif    
    /// Executes step definitions in specified lines from source document
    member this.Execute (sourceUrl:string,lines:string[]) =
        let scenarios = this.GenerateScenarios(sourceUrl,lines)
        scenarios |> Seq.iter (fun action -> action.Action.Invoke())
    member this.Execute (sourceUrl:string,reader:TextReader) =
        this.Execute(sourceUrl, TextReader.readAllLines reader)
    member this.Execute (sourceUrl:string,feature:System.IO.Stream) =
        use reader = new StreamReader(feature)
        this.Execute (sourceUrl,reader)
#if SILVERLIGHT
#else 
    member this.Execute (path:string) =
        this.Execute(path,File.ReadAllLines(path))
#endif