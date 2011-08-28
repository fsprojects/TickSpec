namespace TickSpec

open System

[<AbstractClass;AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type EventAttribute internal (tag:string) =
    inherit Attribute()
    do  if tag <> null then raise (new NotImplementedException())
    member this.Tag = tag

[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type BeforeScenarioAttribute (tag:string) =
    inherit EventAttribute(tag)
    new() = BeforeScenarioAttribute(null)
  
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]  
type AfterScenarioAttribute (tag:string) =
    inherit EventAttribute(tag)
    new() = AfterScenarioAttribute(null)
    
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type BeforeStepAttribute (tag:string) =
    inherit EventAttribute(tag)
    new() = BeforeStepAttribute(null)
  
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]  
type AfterStepAttribute (tag:string) =
    inherit EventAttribute(tag)
    new() = AfterStepAttribute(null)