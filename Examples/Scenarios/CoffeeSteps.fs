module CoffeeSteps

type CoffeeFixture () = inherit TickSpec.NUnit.FeatureFixture("Coffee.feature")

type Property =
    | CoffeesLeft of int
    | Deposit of int

type Events =
    | ServeCoffee

let mutable properties = []
let mutable events = []
let hold x = properties <- x :: properties
let holds f = properties |> List.exists f

open TickSpec
open NUnit.Framework

let [<Given>] ``there are (\d+) coffees left in the machine`` (n:int) =
    CoffeesLeft(n) |> hold

let [<Given>] ``I have deposited (\d+)\$`` (dollars:int) =
    Deposit(dollars) |> hold
    
let [<When>] ``I press the coffee button`` () =
    if holds (function CoffeesLeft(n) when n>0 -> true | _ -> false) &&
       holds (function Deposit(n) when n>0 -> true | _ -> false)
    then events <- ServeCoffee :: events

let [<Then>] ``I should be served a coffee`` () =
    events |> List.exists ((=) ServeCoffee) |> Assert.IsTrue