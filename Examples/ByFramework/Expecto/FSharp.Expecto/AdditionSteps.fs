namespace Computation

open TickSpec
open Expecto

type CalculatorSteps () =
    let calc = Calculator()

    [<Given>]
    member __.``I have entered (.*) into the calculator`` (n:int) =
        calc.Push(n)
    [<When>]
    member __.``I press add`` () = 
        calc.Add()
    [<Then>]
    member __.``the result should be (.*) on the screen`` (n:int) =
        Expect.equal n calc.Result "Result"