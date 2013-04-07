module AdditionSteps

type AdditionFixture () = inherit TickSpec.NUnit.FeatureFixture("Addition.feature")

open Library
open TickSpec
open NUnit.Framework

let mutable calculator = Calculator()

let [<BeforeScenario>] Setup () =
    calculator <- Calculator()

let [<Given>] ``I have entered (\d+) into the calculator`` (n:int) =
    calculator.Push(n)

let [<When>] ``I press add`` () =
    calculator.Sum() |> ignore

let [<Then>] ``the result should be (\d+) on the screen`` (n:int) =
    Assert.AreEqual(n,calculator.Total)
