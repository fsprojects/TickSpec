module SkillSteps

open TickSpec
open NUnit.Framework

type SkillFixture () = inherit TickSpec.NUnit.FeatureFixture("Skill.feature")

type Technique = Katana | ``Karate-Kick`` | ``Roundhouse-Kick`` with
    override technique.ToString() = Union.CaseName technique
let (|Technique|) = function
    | "katana" -> Katana
    | "karate-kick" -> ``Karate-Kick``
    | "roundhouse-kick" -> ``Roundhouse-Kick``
    | s -> invalidOp("Unknown technique: " + s)

type Danger = Low | High | Extreme with
    override danger.ToString() = Union.CaseName danger
let (|Danger|) = function 
    | "low" -> Low
    | "high" -> High
    | "extreme" -> Extreme
    | s -> invalidOp("Unknown danger: " + s)

let skills opponent =
    match opponent with
    | "a samurai" -> [Katana,High; ``Karate-Kick``,Low] 
    | "Chuck Norris" -> [Katana,Extreme; ``Karate-Kick``,Extreme; ``Roundhouse-Kick``,Extreme]
    | s -> invalidOp("Unknown opponent: " + s)

let techniques (table:Table) =
    [for row in table.Rows ->
        match row with
        | [|Technique technique;Danger danger|] -> technique, danger
        | xs -> invalidOp "Unexpected row columns"]

let mutable allowed = []
let mutable attack = []

let [<Given>] ``the following skills are allowed`` (table:Table) =
    allowed <- [for Technique x in table.Raw |> Seq.map Seq.head -> x]

let [<When>] ``a ninja faces (a samurai|Chuck Norris)`` (opponent) =
    let skills = skills opponent
    attack <- skills |> List.filter (fun (skill,_) -> allowed |> List.exists ((=) skill))

let [<Then>] ``he should expect the following attack techniques`` (ratings:(Technique * Danger)[]) =
    //let ratings = techniques table
    Assert.AreEqual(ratings, attack)