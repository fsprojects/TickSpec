namespace Dependency

open TickSpec

type FeedingSteps(instanceProvider: IInstanceProvider, bowl: DogBowl) =
    [<Given>]
    member __.``I have a (.*) dog`` size =
        instanceProvider.RegisterInstance(typeof<DogSize>,
            match size with
            | "large" -> Large
            | "medium" -> Medium
            | "small" -> Small
            | _ -> failwithf "Unsupported dog size: %s" size)

    [<When>]
    member  __.``I fill the dog bowl with (.*)g of food`` amount =
        bowl.FoodAmount <- amount