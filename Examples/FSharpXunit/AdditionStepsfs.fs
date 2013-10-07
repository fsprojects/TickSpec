namespace global

type Calculator () =
    let mutable values = []
    member this.Push(n) = values <- n :: values
    member this.Add() = values <- [List.sum values]
    member this.Result = values.Head

open Xunit
open TickSpec

type AdditionSteps () =
    let calc = Calculator()
    [<Given>]
    member __.``I have entered (.*) into the calculator`` (n:int) =
        calc.Push(n)
    [<When>]
    member __.``I press add`` () = 
        calc.Add()
    [<Then>]
    member __.``the result should be (.*) on the screen`` (n:int) =        
        Assert.Equal(n, calc.Result)

module Boo =

    let [<Given>] ``something`` () = ()

    let [<Given>] ``something else`` () = ()