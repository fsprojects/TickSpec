namespace Dependency

open TickSpec

type DogSteps(bowl: DogBowl, dog: Dog) =
    [<When>]
    member __. ``The dog eats the food from bowl`` () =
        bowl.Eat(dog.AmountToEat)