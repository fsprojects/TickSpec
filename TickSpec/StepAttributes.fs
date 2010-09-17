namespace TickSpec

open System

/// Base attribute class for step annotations
[<AbstractClass;AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
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

