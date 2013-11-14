module FightSteps

type FightFixture () = inherit TickSpec.NUnit.FeatureFixture("Fight.feature")

open TickSpec
open NUnit.Framework

type Action = Engage | Run | Apologise with
    override action.ToString() = Union.CaseName action
     
let mutable level = ""
let mutable firstTime = false
let mutable actions = []

let [<Given>] ``the ninja has a ([a-z]*) level black-belt`` (arg:string) =
    level <- arg

let [<Given>] ``the ninja has never fought Chuck Norris before`` () =
    firstTime <- true

let [<Given>] ``a ninja with the following experience`` (table:Table) =
    let beltLevel = table.Header |> Seq.findIndex ((=) "belt_level")
    level <- table.Rows.[0].[beltLevel]

let [<When>] ``attacked by (a samurai|Chuck Norris)`` foe =
    actions <- 
        match foe with
        | "a samurai" -> [Engage]
        | "Chuck Norris" -> [yield Run; if firstTime then yield Apologise]
        | foe -> invalidOp("Unknown foe: " + foe)

let [<Then>] ``the ninja should (engage the opponent|run for his life|run away)`` action =
    let expected =
        match action with
        | "engage the opponent" -> Engage
        | "run for his life" | "run away" -> Run
        | s -> invalidOp ("Unknown action: " + action)
    Assert.Contains(expected, actions)

let [<Then>] ``the ninja should apologise`` () =
    Assert.Contains(Apologise, actions)