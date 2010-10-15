namespace TickSpec

open System
      
/// Base attribute class for step annotations
[<AbstractClass;AttributeUsage(AttributeTargets.Method,AllowMultiple=true,Inherited=true)>]
type StepAttribute internal (step:string) =
    inherit Attribute()    
    member this.Step = step
/// Method annotation for given step
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true)>]
type GivenAttribute(step:string) = 
    inherit StepAttribute (step) 
    new () = GivenAttribute(null)  
    new (format,[<ParamArray>] args) = 
        GivenAttribute(String.Format(format,args))
/// Method annotation for when step
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true)>]
type WhenAttribute(step) =  
    inherit StepAttribute (step) 
    new () = WhenAttribute(null)
    new (format,[<ParamArray>] args) = 
        WhenAttribute(String.Format(format,args))
/// Method annotation for then step
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=true)>]
type ThenAttribute(step) = 
    inherit StepAttribute(step)
    new () = ThenAttribute(null)
    new (format,[<ParamArray>] args) = 
        ThenAttribute(String.Format(format,args))

/// Method annotation for parsers of string -> 'a
[<AttributeUsage(AttributeTargets.Method,AllowMultiple=false)>]
type ParserAttribute () =
    inherit Attribute()  