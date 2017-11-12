namespace Dependency

open System

type DogSize =
    | Small
    | Medium
    | Large

type DogBowl () =
    member val FoodAmount = 0u with get, set

    member this.Eat amount =
        if amount > this.FoodAmount then raise (ArgumentException("The amount of food to eat has to be less or equal to the amount in bowl", "amount"))
        this.FoodAmount <- this.FoodAmount - amount

type Dog (size: DogSize) =
    member this.AmountToEat =
        match size with
        | Small -> 100u
        | Medium -> 200u
        | Large -> 400u