module TicTacToeStepDefinitions

open TicTacToe
open TickSpec
open Microsoft.VisualStudio.TestTools.UnitTesting

let inline invalidCast s = raise (new System.InvalidCastException(s))

let parseMark (s:string) =
    match s.Trim() with
    | "O" -> Some O
    | "X" -> Some X
    | "" -> None
    | s -> invalidCast s

let (|Col|) = function 
    | "left" -> 0 | "middle" -> 1 | "right" -> 2
    | s -> invalidCast s

let (|Row|) = function 
    | "top" -> 0 | "middle" -> 1 | "bottom" -> 2 
    | s -> invalidCast s

let [<Given>] ``a board layout:`` (table:Table) =
    table.Rows |> Seq.iteri (fun y row -> 
        row |> Seq.iteri (fun x value -> board.[x,y] <- parseMark value)
    )
   
let [<When>] ``a player marks (X|O) at (top|middle|bottom) (left|middle|right)``
        (mark:string,Row row,Col col) =
    board.[col,row] <- parseMark mark

let [<Then>] ``(X|O) wins`` (mark:string) =
    Game.mark <- parseMark mark |> Option.get
    let line = winningLine()
    Assert.IsTrue(line.IsSome)
   
    

