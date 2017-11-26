namespace TickSpec

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open TickSpec.FeatureParser
open TickSpec.ScenarioRun

/// Encapsulates step definitions for execution against features
type StepDefinitions (givens,whens,thens,events,valueParsers) =
    /// Returns method's step attribute or null
    static let getStepAttributes (m:MemberInfo) =
        Attribute.GetCustomAttributes(m,typeof<StepAttribute>)
    static let isMethodInScope (feature:string) (scenario:ScenarioSource) (scopedTags,scopedFeatures,scopedScenarios,m) =
        let trim p (s:string) =
            if s.StartsWith p then (s.Substring p.Length).Trim() else s
        let tagged =
            match scopedTags with
            | [] -> true
            | _ -> scopedTags |> List.exists (fun tag -> scenario.Tags |> Seq.exists ((=) tag))
        let featured =
            let feature = trim "Feature:" feature
            match scopedFeatures with
            | [] -> true
            | _ -> scopedFeatures |> List.exists ((=) feature)
        let scenarioed =
            let name =
                trim "Scenario Outline:" scenario.Name |> trim "Scenario:"
            match scopedScenarios with
            | [] -> true
            | _ -> scopedScenarios |> List.exists ((=) name)
        featured && scenarioed && tagged
    /// Chooses matching definitions for specifed text
    let chooseDefinitions feature scenario text definitions =
        let chooseDefinition pattern =
            // Ensure the full line matches (tolerating leading whitespace and
            // trailing whitespace + punctuation) to stop ambigious matches on
            // overlapping definition
            // (start of line, optional whitespace, definition, optional whitespace/punctuation, end of line)
            let pattern = sprintf "^\s*%s[\s\.,]*$" pattern
            let r = Regex.Match(text,pattern)
            if r.Success then Some r else None
        definitions
        |> List.filter (fun (_,m) -> m |> isMethodInScope feature scenario)
        |> List.choose (fun (pattern:string,(_,_,_,m):string list * string list * string list * MethodInfo) ->
            chooseDefinition pattern |> Option.map (fun r -> r,m)
        )
    /// Chooses defininitons for specified step and text
    let matchStep feature scenario = function
        | GivenStep text -> chooseDefinitions feature scenario text givens
        | WhenStep text -> chooseDefinitions feature scenario text whens
        | ThenStep text -> chooseDefinitions feature scenario text thens
    /// Extract arguments from specified match
    let extractArgs (r:Match) =
        let args = List<string>()
        for i = 1 to r.Groups.Count-1 do
            r.Groups.[i].Value |> args.Add
        args.ToArray()
    /// Resolves line
    let resolveLine feature (scenario:ScenarioSource) (step,line) =
        let matches = matchStep feature scenario step
        let fail e =
            let m = sprintf "%s on line %d in %s\r\n\t\"%s\"" e line.Number (feature.Trim()) (line.Text.Trim())
            StepException(m,line.Number,scenario.Name) |> raise
        if matches.IsEmpty then fail "Missing step definition"
        if matches.Length > 1 then
            let ms = matches |> List.map (fun (m,mi) -> sprintf "%s.%s" mi.DeclaringType.Name mi.Name)
            fail <| sprintf "Ambiguous step definition (%s)" (String.concat "|" ms)
        let r,m = matches.Head
        let tableCount = line.Table |> Option.count
        let bulletsCount = line.Bullets |> Option.count
        let docCount = line.Doc |> Option.count
        let argCount = r.Groups.Count-1+tableCount+bulletsCount+docCount
        if m.GetParameters().Length < argCount then
            fail "Parameter count mismatch"
        line,m,extractArgs r
    /// Chooses in scope events
    let chooseInScopeEvents feature (scenario:ScenarioSource) =
        let choose xs =
            xs
            |> Seq.filter (fun m -> m |> isMethodInScope feature scenario)
            |> Seq.map (fun (_,_,_,e) -> e)
        events
        |> fun (ea,eb,ec,ed) -> choose ea, choose eb, choose ec, choose ed
    /// Gets description as scenario lines
    let getDescription steps =
            steps
            |> Seq.map (fun (_,line) -> line.Text)
            |> String.concat "\r\n"
    new () =
        StepDefinitions(Assembly.GetCallingAssembly())
    /// Constructs instance by reflecting against specified assembly
    new (assembly:Assembly) =
        StepDefinitions(assembly.GetTypes())
    /// Constructs instance by reflecting against specified types
    new (types:Type[]) =
        let getScope attributes =
            attributes
            |> Seq.cast
            |> Seq.fold (fun (tags,features,scenarios) (x:StepScopeAttribute) ->
                x.Tag::tags, x.Feature::features, x.Scenario::scenarios
            ) ([],[],[])
        let methods =
            types
            |> Seq.collect (fun t ->
                let attributes = t.GetCustomAttributes(typeof<StepScopeAttribute>,true)
                let tags, features, scenarios = getScope attributes
                t.GetMethods()
                |> Seq.map (fun m ->
                    let attributes = m.GetCustomAttributes(typeof<StepScopeAttribute>,true)
                    let tags', features', scenarios' = getScope attributes
                    tags@tags' |> List.filter (not << String.IsNullOrEmpty),
                        features@features' |> List.filter (not << String.IsNullOrEmpty),
                            scenarios@scenarios' |> List.filter (not << String.IsNullOrEmpty),
                                m
                )
            )
        StepDefinitions(methods)
    internal new (methods:(string list * string list * string list * MethodInfo) seq) =
        /// Step methods
        let givens, whens, thens =
            methods
            |> Seq.map (fun ((_,_,_,m) as sm) -> sm, getStepAttributes m)
            |> Seq.filter (fun (m,ca) -> ca.Length > 0)
            |> Seq.collect (fun ((_,_,_,m) as sm,ca) ->
                ca
                |> Array.map (fun a ->
                    let p =
                        match (a :?> StepAttribute).Step with
                        | null -> m.Name
                        | step -> step
                    p,a,sm
                )
                |> Seq.distinctBy (fun (p,a,m) -> p)
            )
            |> Seq.fold (fun (gs,ws,ts) (p,a,m) ->
                match a with
                | :? GivenAttribute -> ((p,m)::gs,ws,ts)
                | :? WhenAttribute -> (gs,(p,m)::ws,ts)
                | :? ThenAttribute -> (gs,ws,(p,m)::ts)
                | _ -> invalidOp "Unhandled StepAttribute"
            ) ([],[],[])

        let filter (t:Type) (elements:(string list * string list * string list * MethodInfo) seq) =
            elements |> Seq.filter (fun (_,_,_,m) -> null <> Attribute.GetCustomAttribute(m,t))
        /// Step events
        let events = methods |> filter typeof<EventAttribute>
        let beforeScenario = events |> filter typeof<BeforeScenarioAttribute>
        let afterScenario = events |> filter typeof<AfterScenarioAttribute>
        let beforeStep = events |> filter typeof<BeforeStepAttribute>
        let afterStep = events |> filter typeof<AfterStepAttribute>
        let events = beforeScenario, afterScenario, beforeStep, afterStep
        /// Parser methods
        let valueParsers =
            methods
            |> filter typeof<ParserAttribute>
            |> Seq.map (fun (_,_,_,m) -> m.ReturnType, m)
            |> Dict.ofSeq
        StepDefinitions(givens,whens,thens,events,valueParsers)

    member val private InstanceProviderFactory : unit -> IInstanceProvider
        = fun () -> new InstanceProvider() :> _
        with get, set

    member this.ServiceProviderFactory
        with set providerFactory =
            this.InstanceProviderFactory <- fun () -> new ServiceProviderWrapper(providerFactory()) :> IInstanceProvider

    /// Generate scenarios from specified lines (source undefined)
    member this.GenerateScenarios (lines:string []) =
        let featureSource = parseFeature lines
        let feature = featureSource.Name
        featureSource.Scenarios
        |> Seq.map (fun scenario ->
            let steps =
                scenario.Steps
                |> Seq.map (resolveLine feature scenario)
                |> Seq.toArray
            let events = chooseInScopeEvents feature scenario
            let action = generate events valueParsers (scenario.Name, steps) this.InstanceProviderFactory
            {Name=scenario.Name;Description=getDescription scenario.Steps;
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
        let feature = featureSource.Name
        let gen = FeatureGen(featureSource.Name,sourceUrl)
        let genType scenario =
            let lines =
                scenario.Steps
                |> Seq.map (resolveLine feature scenario)
                |> Seq.toArray
            let events = chooseInScopeEvents feature scenario
            gen.GenScenario
                events
                valueParsers
                (scenario.Name, lines, scenario.Parameters)
        let createAction scenario =
            let t = lazy (genType scenario)
            TickSpec.Action(fun () ->
                let ctor = t.Force().GetConstructor([| typeof<FSharpFunc<unit, IInstanceProvider>> |])
                let instance = ctor.Invoke([| this.InstanceProviderFactory |])
                let mi = instance.GetType().GetMethod("Run")
                mi.Invoke(instance,[||]) |> ignore
            )
        let scenarios =
            featureSource.Scenarios
            |> Seq.map (fun scenario ->
                let action = createAction scenario
                { Name=scenario.Name;Description=getDescription scenario.Steps;
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
    member this.GenerateFeature (path:string) =
        this.GenerateFeature(path,File.ReadAllLines(path))
    /// Generates scenarios in specified lines from source document
    member this.GenerateScenarios (sourceUrl:string,lines:string[]) =
        this.GenerateFeature(sourceUrl,lines).Scenarios
    member this.GenerateScenarios (sourceUrl:string,reader:TextReader) =
        this.GenerateScenarios(sourceUrl, TextReader.readAllLines reader)
    member this.GenerateScenarios (sourceUrl:string,feature:System.IO.Stream) =
        if feature = null then
            let message =
                "No stream found for resource: " + sourceUrl +
                ". Perhaps there was a typo, or the feature file " +
                "wasn't compiled as an EmbeddedResource?"
            raise (new ArgumentNullException("feature", message))
        use reader = new StreamReader(feature)
        this.GenerateScenarios(sourceUrl, reader)
    member this.GenerateScenarios (path:string) =
        this.GenerateScenarios(path,File.ReadAllLines(path))
    /// Executes step definitions in specified lines from source document
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