[<TickSpec.StepScope(Feature="Scrabble score")>]
module ScrabbleSteps

type ScrabbleFixture () = inherit TickSpec.NUnit.FeatureFixture("Scrabble.feature")

let points = function
    | 'A' | 'E' | 'I' | 'L' | 'N' | 'O' | 'R' | 'S' | 'T' | 'U' -> 1
    | 'D' | 'G' -> 2
    | 'B' | 'C' | 'M' | 'P' -> 3
    | 'F' | 'H' | 'V' | 'W' | 'Y' -> 4
    | 'K' -> 5
    | 'J' | 'X' -> 8
    | 'Q' | 'Z' -> 10
    | a -> invalidOp <| sprintf "Letter %c" a

type Property =
    | Word of string
    | DLS of char
    | TWS
    | CenterStar

let total properties =    
    properties |> List.fold (fun (n,m) p ->
        match p with
        | Word(word) -> (n + (word.ToCharArray() |> Array.sumBy points)), m
        | DLS(letter) -> n + points letter, m
        | TWS -> (n, m*3)
        | CenterStar -> (n, m*2)
    ) (0,1)
    |> fun (n,m) -> n*m

open TickSpec
open NUnit.Framework

let mutable properties = []
let hold p = properties <- p::properties

let [<BeforeScenario>] SetupScenario () = properties <- []
let [<Given>] ``an empty scrabble board`` () = 
    ()
let [<When>] ``player (\d+) plays "([A-Z]+)" at (\d+[A-Z])`` (player:int,word:string,location:string) = 
    hold(Word(word))
let [<When>] ``player (\d+) prefixes "([A-Z]+)" with "([A-Z]+)" at (\d+[A-Z])``
    (player:int,prefix:string,word:string,location:string) =
    hold(Word(prefix+word))
let [<When>] ``([A-Z]) is on a DLS`` (letter:char) =
    hold(DLS(letter))
let [<When>] ``([A-Z]) is on a TWS`` (letter:char) =
    hold(TWS)
let [<When>] ``([A-Z]) is on the center star`` (letter:char) =
    hold(CenterStar)
let [<Then>] ``he scores (\d+)`` (score:int) =
    Assert.AreEqual(score, total properties)