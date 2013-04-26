[<TickSpec.StepScope(Feature="Scrabble score")>]
module ScrabbleSteps
type ScrabbleFixture () = inherit TickSpec.NUnit.FeatureFixture("Scrabble.feature")

open Scrabble
open TickSpec
open NUnit.Framework

let mutable properties = []
let hold p = properties <- p::properties

let [<BeforeScenario>] SetupScenario () = properties <- []
let [<Given>] ``an empty scrabble board`` () = ()
let [<When>] ``player (\d+) plays "([A-Z]+)" at (\d+[A-Z])`` 
    (player:int,word:string,location:string) = 
    hold(Word(word))
let [<When>] ``player (\d+) prefixes "([A-Z]+)" with "([A-Z]+)" at (\d+[A-Z])``
    (player:int,prefix:string,word:string,location:string) =
    hold(Word(prefix+word))
let [<When>] ``forms ([A-Z]+)`` (word:string) =
    hold(Word(word))
let [<When>] ``([A-Z]) is on a DLS`` (letter:char) =
    hold(DLS(letter))
let [<When>] ``([A-Z]) is on a TWS`` (letter:char) =
    hold(TWS)
let [<When>] ``([A-Z]) is on the center star`` (letter:char) =
    hold(CenterStar)
let [<Then>] ``he scores (\d+)`` (score:int) =
    Assert.AreEqual(score, total properties)