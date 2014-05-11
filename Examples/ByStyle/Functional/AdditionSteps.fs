module AdditionSteps

type Calculator = { Values : int list } with
    member this.Push n = {Values=n::this.Values}
    member this.Add() = {Values=[List.sum this.Values]}
    member this.Top = List.head this.Values
    static member Create() = { Values = [] }

#if INTERACTIVE
#r @"..\..\..\Nuget\dotNet\Lib\net40\TickSpec.dll"
#load "Functional.fs"
module Assert =
    let AreEqual (expected,actual) = 
        if expected <> actual then 
            let msg = sprintf "\r\nExpected: %A\r\nBut was: %A" expected actual
            failwith msg
#else
open NUnit.Framework
#endif

open TickSpec.Functional

let performStep (calc:Calculator) (step) =
    match step with
    | Given "I have entered (.*) into the calculator" [Int n] ->
        calc.Push n              
    | When "I press add" [] ->
        calc.Add ()
    | Then "the result should be (.*) on the screen" [Int n] ->
        Assert.AreEqual(n,calc.Top)
        calc
    | _ -> notImplemented()                

#if INTERACTIVE
let featureFile =  __SOURCE_DIRECTORY__ + "\Addition.txt"
run featureFile performStep Calculator.Create
#else
open TickSpec.NUnit
type AdditionFeature () =
    inherit FeatureFixture<Calculator>("Addition.txt", performStep, Calculator.Create)
#endif
