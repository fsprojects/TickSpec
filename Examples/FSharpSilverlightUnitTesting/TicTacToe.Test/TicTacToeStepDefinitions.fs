module TicTacToeStepDefinitions

open TicTacToe
open TickSpec
open Microsoft.VisualStudio.TestTools.UnitTesting

let inline invalidCast s = raise (new System.InvalidCastException(s))

let (|Mark|) = function
    | "O" -> Some O | "X" -> Some X | "" -> None
    | s -> invalidCast s

let (|Col|) = function 
    | "left" -> 0 | "middle" -> 1 | "right" -> 2
    | s -> invalidCast s

let (|Row|) = function 
    | "top" -> 0 | "middle" -> 1 | "bottom" -> 2 
    | s -> invalidCast s

let [<Given>] ``a board layout:`` (table:Table) =
    table.Rows |> Seq.iteri (fun y row -> 
        row |> Seq.iteri (fun x (Mark value) -> board.[x,y] <- value)
    )
   
let [<When>] ``a player marks (X|O) at (top|middle|bottom) (left|middle|right)``
        (Mark mark,Row row,Col col) =
    board.[col,row] <- mark

let [<When>] ``viewed with a (.*) degree rotation`` (degrees:int) =
    for i = 0 to (degrees/90)-1 do
        let rotated = Array2D.init 2 2 (fun x y -> board.[2-x,y])
        Array2D.blit rotated 0 0 board 0 0 2 2

let [<Then>] ``(X|O) wins`` (Mark mark) =
    Game.mark <- mark.Value
    let line = winningLine()
    Assert.IsTrue(line.IsSome)
   
    

