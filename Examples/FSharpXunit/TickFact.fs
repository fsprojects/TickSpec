namespace TickSpec

open System
open Xunit
open Xunit.Sdk
open System.Xml

type TickCommand(scenario:Scenario,info:IMethodInfo) = 
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

[<AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)>] 
type TickFactAttribute() = 
    inherit FactAttribute() 
    override this.EnumerateTestCommands (info:IMethodInfo) =         
        let mi = info.MethodInfo
        let scenarios = mi.Invoke(null, null) :?> seq<Scenario> 
        scenarios
        |> Seq.map (fun scenario -> TickCommand(scenario,info))
        |> Seq.cast                       