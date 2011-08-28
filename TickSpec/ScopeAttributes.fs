namespace TickSpec

open System

[<AttributeUsage(AttributeTargets.Class,AllowMultiple=true,Inherited=true)>]
type BindingAttribute() =
    inherit Attribute()    

[<AttributeUsage(
    AttributeTargets.Class|||AttributeTargets.Method,
    AllowMultiple=true,
    Inherited=true)>]
type StepScopeAttribute() =
    inherit Attribute()
    let mutable tag : string = null
    let mutable feature : string = null
    member scope.Tag
        with get () = tag
        and set value = tag <- value
    member scope.Feature
        with get () = feature
        and set value = feature <- value