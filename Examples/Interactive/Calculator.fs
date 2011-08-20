module Calculator.Test

#nowarn "25" // warning FS0025: Incomplete pattern matches on this expression.


let (|Int|) = function s -> System.Int32.Parse(s)

type Calculator () =
    let mutable values = []
    member this.Push(n) = values <- n :: values
    member this.Add() = values <- [List.sum values]
    
let mutable calculator = Calculator()

do    
    Given "I have entered (.*) into the calculator" <| fun [Int n] ->         
        calculator <- Calculator()
        calculator.Push n    
    
    When "I press add" <| fun [] ->
        invalidOp "Bang"

    Then "the result should be (.*) on the screen" <| fun [Int n] ->
        pending()

    "Feature: Addition
     In order to avoid silly mistakes
     As a math idiot
     I want to be told the sum of two numbers

    Scenario: Add two numbers
     Given I have entered 50 into the calculator
      | 10 | 20 |
     And I have entered 70 into the calculator
     When I press add
     Then the result should be 120 on the screen
    "
    |> toLines
    |> Execute
    System.Console.ReadLine() |> ignore
