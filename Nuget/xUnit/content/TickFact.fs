namespace TickSpec

open System
open Xunit
open Xunit.Sdk
open System.Xml

/// TickCommand executes TickSpec scenarios
type internal TickCommand(scenario:Scenario,info:IMethodInfo) = 
    interface ITestCommand with 
        member this.Timeout = 0
        member this.ShouldCreateInstance = false 
        member this.Execute testClass = 
            try 
                scenario.Action.Invoke()
                PassedResult(info, scenario.ToString()) :> MethodResult 
            with ex -> 
                FailedResult(info, ex, scenario.ToString()) :> MethodResult 
             
        member this.DisplayName = scenario.Name 
        member this.ToStartXml () = 
            let doc = XmlDocument() 
            doc.LoadXml("<dummy/>") 
            let testNode = XmlUtility.AddElement(doc.ChildNodes.[0], "start") 
            XmlUtility.AddAttribute(testNode, "name", scenario.Name) 
            XmlUtility.AddAttribute(testNode, "type", info.MethodInfo.ReflectedType.FullName) 
            XmlUtility.AddAttribute(testNode, "method", info.Name) 
            testNode 

/// Annotation for methods generating TickSpec scenarios
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)>] 
type TickFactAttribute() = 
    inherit FactAttribute() 
    override this.EnumerateTestCommands (info:IMethodInfo) =
        let mi = info.MethodInfo
        let scenarios = mi.Invoke(null, null) :?> seq<Scenario> 
        scenarios
        |> Seq.map (fun scenario -> TickCommand(scenario,info))
        |> Seq.cast

module Features =
    open System.IO
    open System.Reflection

    let assembly = Assembly.GetExecutingAssembly()
    let definitions = new StepDefinitions(assembly)

    /// Generates scenarios from a feature file
    [<TickFact>]
    let StockFeature () =
        let source = @"StockFeature.txt"
        let s = File.OpenText(Path.Combine(@"..\..", source))
        definitions.GenerateScenarios(source,s)

    //[<TickFact>]
    //let YourFeature () =
    //    let source = @"YourFeature.txt"
    //    let s = File.OpenText(Path.Combine(@"..\..", source))
    //    definitions.GenerateScenarios(source,s)