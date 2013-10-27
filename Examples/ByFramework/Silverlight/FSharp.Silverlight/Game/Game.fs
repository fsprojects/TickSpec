namespace TicTacToe

type Mark = O | X

[<AutoOpen>]
module Game =
    let mutable mark = X
    let alternateMark () = mark <- if mark = O then X else O
    let board : Mark option [,] = Array2D.init 3 3 (fun x y -> None)
    let winningLine () =
        [ 
        for i in 0..2 do
              yield [i, 0; i, 1; i, 2]
              yield [0, i; 1, i; 2, i]
        yield [0, 0; 1, 1; 2, 2]
        yield [0, 2; 1, 1; 2, 0] 
        ]
        |> List.tryFind (fun line ->
            line |> List.forall (fun (x,y) -> board.[x,y] = Some mark)
        )