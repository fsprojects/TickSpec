namespace TickSpec

open System

[<AbstractClass;AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type EventAttribute internal () =
    inherit Attribute()

[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type BeforeScenarioAttribute () =
    inherit EventAttribute()
  
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]  
type AfterScenarioAttribute () =
    inherit EventAttribute()
    
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type BeforeStepAttribute () =
    inherit EventAttribute()
  
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]  
type AfterStepAttribute () =
    inherit EventAttribute()