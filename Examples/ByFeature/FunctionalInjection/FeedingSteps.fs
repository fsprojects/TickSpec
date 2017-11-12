module Dependency.FeedingSteps

open TickSpec

let [<Given>] ``I have a (.*) dog`` (size) =
    match size with
    | "large" -> Large
    | "medium" -> Medium
    | "small" -> Small
    | _ -> failwithf "Unsupported dog size: %s" size

let [<When>] ``I fill the dog bowl with (.*)g of food`` (amount:uint32) =
    { FoodAmount = amount }