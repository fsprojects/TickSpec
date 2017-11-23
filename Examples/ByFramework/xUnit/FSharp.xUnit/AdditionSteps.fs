namespace global

type Calculator () =
    let mutable values = []
    member __.Push(n) = values <- n :: values
    member __.Add() = values <- [List.sum values]
    member __.Result = values.Head

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
