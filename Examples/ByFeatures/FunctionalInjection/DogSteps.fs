module DogSteps

open TickSpec
open Domain
    
let [<When>] ``The dog eats the food from bowl`` (bowl: DogBowl) (dog:Dog) =
        bowl.Eat(dog.AmountToEat)