namespace TickSpec

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open TickSpec.FeatureParser
open TickSpec.ScenarioRun

type internal MethodWithScope =
    // tags * features * scenarios * method
    string list * string list * string list * MethodInfo

type internal MethodScope =
    // tags * features * scenarios
    string list * string list * string list

type internal CategorizedMethods =
    Dictionary<Type, (MethodWithScope * obj) list>

/// Encapsulates step definitions for execution against features
type StepDefinitions (givens,whens,thens,events,valueParsers) =
    let instanceProviderFactory = ref (fun () -> new InstanceProvider() :> IInstanceProvider)
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
    static let chooseDefinitions feature scenario text definitions =
        let chooseDefinition pattern =
            // Ensure the full line matches (tolerating leading whitespace and
            // trailing whitespace + punctuation) to stop ambigious matches on
            // overlapping definition
            // (start of line, optional whitespace, definition, optional whitespace/punctuation, end of line)
            let pattern = sprintf "^\s*%s[\s\.,]*$" pattern
            let r = Regex.Match(text,pattern)
            if r.Success then Some r else None
        definitions
        |> Seq.filter (fun (_,m) -> m |> isMethodInScope feature scenario)
        |> Seq.choose (fun (pattern:string,(_,_,_,m):MethodWithScope) ->
            chooseDefinition pattern |> Option.map (fun r -> r,m)
        )
        |> Seq.toList
    /// Extract arguments from specified match
    static let extractArgs (r:Match) =
        let args = List<string>()
        for i = 1 to r.Groups.Count-1 do
            r.Groups.[i].Value |> args.Add
        args.ToArray()
    /// Gets description as scenario lines
    static let getDescription steps =
        steps
        |> Seq.map (fun (_,line) -> line.Text)
        |> String.concat "\r\n"
    /// Chooses definitions for specified step and text
    let matchStep feature scenario = function
        | GivenStep text -> chooseDefinitions feature scenario text givens
        | WhenStep text -> chooseDefinitions feature scenario text whens
        | ThenStep text -> chooseDefinitions feature scenario text thens
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
            [|
                for _,_,_,e as x in xs do
                    if isMethodInScope feature scenario x then
                        yield e
            |]
        events
        |> fun (ea,eb,ec,ed) -> choose ea, choose eb, choose ec, choose ed
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
    internal new (methods:MethodInfo seq) =
        let categorizedMethods =
            let getScope
                    attributes
                    (parentTags: string list)
                    (parentFeatures: string list)
                    (parentScenarios: string list) =
                let add x s = if s |> String.IsNullOrEmpty |> not then s::x else x

                attributes
                |> Seq.cast<StepScopeAttribute>
                |> Seq.fold (fun (tags, features, scenarios) attr ->
                    add tags attr.Tag,
                    add features attr.Feature,
                    add scenarios attr.Scenario
                ) (parentTags, parentFeatures, parentScenarios)
            
            let attributeMap = Dictionary<Type, (MethodWithScope * obj) list>()
            let parentScope = Dictionary<Type, MethodScope>()
            let methodScope = Dictionary<MethodInfo, MethodScope>()

            let attributes = [|
                typeof<GivenAttribute>; typeof<WhenAttribute>; typeof<ThenAttribute>
                typeof<BeforeScenarioAttribute>; typeof<AfterScenarioAttribute>
                typeof<BeforeStepAttribute>; typeof<AfterStepAttribute>
                typeof<ParserAttribute>
            |]

            // Initialize the attribute map
            attributes |> Array.iter (fun attrType -> attributeMap.[attrType] <- List.empty)

            // Iterate through all methods
            methods |> Seq.iter (fun mi ->
                // Get all attributes of the method
                mi.GetCustomAttributes(true)
                |> Array.iter (fun attr ->
                    let usedType = attr.GetType()
                    let correspondingAttrType =
                        attributes
                        |> Array.tryFind (fun x -> x.IsAssignableFrom(usedType))

                    match correspondingAttrType with
                    // In case it is one of the ones we care about, we add it to the map
                    | Some attrType ->
                        // We use caching to not repeatedly get the scope for methods or its declaring types
                        if methodScope.ContainsKey mi |> not then
                            let parentType = mi.DeclaringType
                            if parentScope.ContainsKey parentType |> not then
                                let parentScopeAttribute = parentType.GetCustomAttributes(typeof<StepScopeAttribute>,true)
                                parentScope.[parentType] <- getScope parentScopeAttribute List.empty List.empty List.empty
                            let parentTags, parentFeatures, parentScenarios = parentScope.[parentType]
                            let methodScopeAttribute = mi.GetCustomAttributes(typeof<StepScopeAttribute>,true)
                            methodScope.[mi] <- getScope methodScopeAttribute parentTags parentFeatures parentScenarios

                        let tags, features, scenarios = methodScope.[mi]

                        let existingPairs = attributeMap.[attrType]
                        attributeMap.[attrType] <- ((tags, features, scenarios, mi), attr)::existingPairs
                    | None -> ()
                )
            )

            attributeMap

        /// Step methods
        let extractStepAttribute stepAttribute =
            categorizedMethods.[stepAttribute]
            |> Seq.map (fun ((_,_,_,m) as method, attr) ->
                let p =
                    match (attr :?> StepAttribute).Step with
                    | null -> m.Name
                    | step -> step
                p,method
            )
            |> Array.ofSeq

        let givens = typeof<GivenAttribute> |> extractStepAttribute
        let whens = typeof<WhenAttribute> |> extractStepAttribute
        let thens = typeof<ThenAttribute> |> extractStepAttribute

        /// Step events
        let filterEvents eventAttribute =
            categorizedMethods.[eventAttribute]
            |> Seq.map fst
            |> Array.ofSeq

        let beforeScenario = typeof<BeforeScenarioAttribute> |> filterEvents
        let afterScenario = typeof<AfterScenarioAttribute> |> filterEvents
        let beforeStep = typeof<BeforeStepAttribute> |> filterEvents
        let afterStep = typeof<AfterStepAttribute> |> filterEvents
        let events = beforeScenario, afterScenario, beforeStep, afterStep

        /// Parser methods
        let valueParsers =
            categorizedMethods.[typeof<ParserAttribute>]
            |> Seq.map (fun ((_,_,_,m),_) -> m.ReturnType, m)
            |> Dict.ofSeq
        StepDefinitions(givens,whens,thens,events,valueParsers)

    /// Provides a mechanism to customize the creation of
    /// - StepDefinition classes
    /// - Items that need their lifetimes managed at the Scenario run level (TickSpec will Dispose the ServiceProvider at the end of a Test Run)
    // - Items that should be shared at Test Run level (e.g. Database fixtures might need to establish/tear down once per test run using xUnit mechanisms in order to avoid polluting the Steps to achieve that)
    member __.ServiceProviderFactory
        with set providerFactory =
            let mkScenarioContainer () : IInstanceProvider =
                new ExternalServiceProviderInstanceProvider(providerFactory()) :> _
            instanceProviderFactory.Value <- mkScenarioContainer

    /// Generate scenarios from specified lines (source undefined)
    member __.GenerateScenarios (lines:string []) =
        let featureSource = parseFeature lines
        let feature = featureSource.Name
        featureSource.Scenarios
        |> Seq.map (fun scenario ->
            let steps =
                scenario.Steps
                |> Seq.map (resolveLine feature scenario)
                |> Seq.toArray
            let events = chooseInScopeEvents feature scenario
            let scenarioMetadata =
                {Name=scenario.Name;Description=getDescription scenario.Steps;Parameters=scenario.Parameters;Tags=scenario.Tags}
            
            TickSpec.Action(generate events valueParsers (scenarioMetadata, steps) instanceProviderFactory)
            |> Scenario.fromScenarioMetadata scenarioMetadata
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
    member __.GenerateFeature (sourceUrl:string,lines:string[]) =
        let featureSource = parseFeature lines
        let feature = featureSource.Name
#if !NETSTANDARD2_0
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
        let createAction scenario (scenarioMetadata: ScenarioMetadata) =
            let t = lazy (genType scenario)
            TickSpec.Action(fun () ->
                let ctor = t.Force().GetConstructor([|
                    typeof<FSharpFunc<unit, IInstanceProvider>>
                    typeof<ScenarioMetadata>
                |])

                let instance = ctor.Invoke([|
                    instanceProviderFactory.Value
                    scenarioMetadata
                |])

                let mi = instance.GetType().GetMethod("Run")
                mi.Invoke(instance,[||]) |> ignore
            )
        let scenarios =
            featureSource.Scenarios
            |> Seq.map (fun scenario ->
                let scenarioMetadata =
                    { Name=scenario.Name;Description=getDescription scenario.Steps;Parameters=scenario.Parameters;Tags=scenario.Tags }
                createAction scenario scenarioMetadata
                |> Scenario.fromScenarioMetadata scenarioMetadata
            )
        let assembly = gen.Assembly
#else
        let scenarios = __.GenerateScenarios lines
        let assembly = null
#endif
        { Name = featureSource.Name;
          Source = sourceUrl;
          Assembly = assembly;
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