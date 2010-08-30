namespace TickSpec

module internal Seq =
    /// Skips elements in sequence until condition is met  
    let skipUntil (p:'a -> bool) (source:seq<_>) =  
        seq { 
            use e = source.GetEnumerator() 
            let latest = ref (Unchecked.defaultof<_>)
            let ok = ref false
            while e.MoveNext() do
                if (latest := e.Current; (!ok || p !latest)) then
                    ok := true
                    yield !latest 
        }

module internal TextReader =
    /// Reads lines from TextReader
    let readLines (reader:System.IO.TextReader) =
        seq {                  
            let isEOF = ref false
            while not !isEOF do
                let line = reader.ReadLine()                
                if line <> null then yield line
                else isEOF := true
        }

open System

/// Base attribute class for step annotations
[<AbstractClass;AttributeUsage(AttributeTargets.Method,Inherited=true)>]
type StepAttribute internal (step:string) =
    inherit System.Attribute ()
    internal new () = StepAttribute(null)    
    member this.Step = step
/// Method annotation for given step
type GivenAttribute(step:string) = 
    inherit StepAttribute (step) 
    new () = GivenAttribute(null)        
/// Method annotation for when step
type WhenAttribute(step) =  
    inherit StepAttribute (step) 
    new () = WhenAttribute(null) 
/// Method annotation for then step
type ThenAttribute(step) = 
    inherit StepAttribute(step)
    new () = ThenAttribute(null)
/// Line state
type internal Line = ScenarioStart | GivenStep | WhenStep | ThenStep

/// Step specific exception
type StepException (message,line:int,scenario:string) =
    inherit System.Exception(message)
    member this.LineNumber = line
    member this.Scenario = string

open System.Collections
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions

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
    /// Invokes method with match values as arguments
    let invoke (r:Match,m:MethodInfo) =
        let ps = m.GetParameters()        
        let args = ArrayList()         
        for i = 1 to r.Groups.Count-1 do
            let arg = r.Groups.[i].Value |> box
            let value = Convert.ChangeType(arg,ps.[i-1].ParameterType)
            value |> args.Add |> ignore        
        let instance =
            if m.IsStatic then null                
            else getInstance m.DeclaringType
        m.Invoke(instance, args.ToArray()) |> ignore        
    /// Execute scenarios in specified lines
    let executeScenarios lines =
        lines
        |> Seq.fold (fun (lastStep,scenario) (n,line) ->               
            let step, text = matchLine (lastStep,line)                                
            match step with
            | ScenarioStart -> step, text
            | GivenStep | WhenStep | ThenStep ->
                let matches = matchStep text step           
                let fail e = StepException(e,n,scenario) |> raise
                if matches.IsEmpty then fail "Missing step"                     
                if matches.Length > 1 then fail "Ambiguous step"                                    
                let r,m = matches.Head 
                if m.GetParameters().Length <> r.Groups.Count-1 then
                    fail "Parameters mismatch"                
                (r,m) |> invoke   
                step, scenario                     
        ) (ScenarioStart,"")
        |> ignore
    /// Execute feature lines
    let execute (featureLines:seq<string>) =
        featureLines
        |> Seq.mapi (fun i line -> (i+1,line))      
        |> Seq.skipUntil (fun (_,line) -> line.Trim().StartsWith("Scenario"))
        |> Seq.filter (fun (_,line) -> line.Trim().Length > 0)          
        |> executeScenarios
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
    /// Execute step definitions matching feature lines
    member this.Execute (featureText:string) =
        use reader = new StringReader(featureText)
        reader |> TextReader.readLines |> execute
    /// Execute step definitions matching feature lines
    member this.Execute (feature:System.IO.Stream) =        
        use reader = new StreamReader(feature)
        reader |> TextReader.readLines |> execute