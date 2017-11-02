module PersonSteps

open TickSpec
open Domain

let [<Given>] ``I have a (.*) dog`` (size) =
    match size with
    | "large" -> Dog(Large)
    | "medium" -> Dog(Medium)
    | "small" -> Dog(Small)
    | _ -> failwithf "Unsupported dog size: %s" size

let [<When>] ``I fill the dog bowl with (.*)g of food`` (amount:uint32) =
    { FoodAmount = amount }