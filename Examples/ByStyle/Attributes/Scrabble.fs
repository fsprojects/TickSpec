module Scrabble

let letterPoints = function
    | 'A' | 'E' | 'I' | 'L' | 'N' | 'O' | 'R' | 'S' | 'T' | 'U' -> 1
    | 'D' | 'G' -> 2
    | 'B' | 'C' | 'M' | 'P' -> 3
    | 'F' | 'H' | 'V' | 'W' | 'Y' -> 4
    | 'K' -> 5
    | 'J' | 'X' -> 8
    | 'Q' | 'Z' -> 10
    | a -> invalidOp <| sprintf "Letter %c" a

let wordPoints (word:string) =
    word.ToCharArray() |> Array.sumBy letterPoints

type Property =
    | Word of string
    | DLS of char
    | TWS
    | CenterStar

let total properties =
    properties |> List.fold (fun (n,m) p ->
        match p with
        | Word(word) when word.Length = 7 -> 50 + n + wordPoints word, m
        | Word(word) -> n + wordPoints word, m
        | DLS(letter) -> n + letterPoints letter, m
        | TWS -> (n, m*3)
        | CenterStar -> (n, m*2)
    ) (0,1)
    |> fun (n,m) -> n*m