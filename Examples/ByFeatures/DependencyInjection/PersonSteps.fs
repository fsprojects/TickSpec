namespace Dependency

open TickSpec
open NUnit.Framework

type PersonSteps(instanceProvider: IInstanceProvider, bowl: DogBowl) =
    [<Given>]
    member this.``I have a (.*) dog`` (size) =
        match size with
        | "large" -> instanceProvider.RegisterInstance typeof<DogSize> Large
        | "medium" -> instanceProvider.RegisterInstance typeof<DogSize> Medium
        | "small" -> instanceProvider.RegisterInstance typeof<DogSize> Small
        | _ -> Assert.Fail(sprintf "Unsupported dog size: %s" size)

    [<When>]
    member this.``I fill the dog bowl with (.*)g of food`` (amount:uint32) =
        bowl.FoodAmount <- amount