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
    | CenterStar

open TickSpec
open NUnit.Framework

let mutable properties = []
let hold p = properties <- p::properties

let [<Given>] ``an empty scrabble board`` () = ()
 
let [<When>] ``player (.*) plays "(.*)" at (.*)`` (player:int,word:string,location:string) = 
    hold(Word(word))

let [<When>] ``(.*) is on a DLS`` (letter:char) =
    hold(DLS(letter))

let [<When>] ``(.*) is on the center star`` (letter:char) =
    hold(CenterStar)
 
let [<Then>] ``he scores (.*)`` (score:int) =
    let n,m =
        properties |> List.fold (fun (n,m) p ->
            match p with
            | Word(word) -> (n + (word.ToCharArray() |> Array.sumBy points)), m
            | DLS(letter) -> n + points letter, m
            | CenterStar -> (n, m*2)
        ) (0,1)
    Assert.AreEqual(score, n*m)