namespace global

[<AutoOpen>]
module NUnitExtensions =
    open NUnit.Framework
    type Assert with
        static member Contains(expected:obj, xs:'a list) =
            Assert.Contains(expected, xs |> List.toArray)

module Union =
    open Microsoft.FSharp.Reflection
    let CaseName (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name