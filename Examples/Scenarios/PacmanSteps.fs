[<TickSpec.StepScope(Feature="Pacman score")>]
module PacmanSteps

type CoffeeFixture () = inherit TickSpec.NUnit.FeatureFixture("Pacman.feature")

type Property =
    | PacDot
    | PowerPellet
    | Ghosts of int
    | Fruit of string

open TickSpec
open NUnit.Framework

let mutable properties = []

let [<BeforeScenario>] SetupScenario () = 
    properties <- []

let [<Given>] ``the ghosts are vulnerable`` () = ()

let [<When>] ``pacman eats a pac-dot`` () =
    properties <- PacDot::properties

let [<When>] ``pacman eats a power pellet`` () =
    properties <- PowerPellet::properties

let [<When>] ``pacman eats (.*) in succession`` (ghosts:int) =
    properties <- Ghosts(ghosts)::properties

let [<When>] ``pacman easts a (.*)`` (fruit:string) =
    properties <- Fruit(fruit)::properties

let [<Then>] ``he scores (.*)`` (points:int) =
    let scored =
        properties |> List.sumBy (function
            | PacDot -> 10
            | PowerPellet -> 50
            | Ghosts(0) -> 0
            | Ghosts(1) -> 200
            | Ghosts(2) -> 400
            | Ghosts(3) -> 800
            | Ghosts(4) -> 1600
            | Ghosts(n) -> 1600
            | Fruit(name) -> points
        )
    Assert.AreEqual(points, scored)