[<AutoOpen>]
module TickSepc.Build.Tests.Assertions

open System.Text.RegularExpressions
open NUnit.Framework.Constraints

type SubStringConstraintIgnoreWhitespaces(expected:string) =
    inherit Constraint()
    let removeWhitespaces str = Regex.Replace(str, @"\s", "")
    let expected' = expected |> removeWhitespaces
    override this.ApplyTo(actual) =
        let actual' = actual.ToString() |> removeWhitespaces
        let isSuccess = actual'.Contains(expected')
        ConstraintResult(this, actual, isSuccess)
    override _.Description = expected

let haveSubstringIgnoringWhitespaces = SubStringConstraintIgnoreWhitespaces

let dump x =
    printfn "%A" x
    x
