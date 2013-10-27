module TicTacToeStepDefinitions

open TickSpec
open MbUnit.Framework

let mutable layout = [|[||]|]

let [<Given>] ``a board layout:`` (table:Table) =
    layout <- table.Rows         

let (|Col|) = function
    | "left" -> 0 | "middle" -> 1 | "right" -> 2
    | _ -> raise (new System.InvalidCastException())

let (|Row|) = function
    | "top" -> 0 | "middle" -> 1 | "bottom" -> 2
    | _ -> raise (new System.InvalidCastException())   

let [<When>] ``a player marks (X|O) at (top|middle|bottom) (left|middle|right)`` 
   (mark:string,Row row,Col col) =
    Assert.IsTrue(System.String.IsNullOrEmpty(layout.[row].[col]))
    layout.[row].[col] <- mark
    
let [<Then>] ``(X|O) wins`` (mark:string) =
    [
        // Check horizontals
        [0..2] |> Seq.exists (fun y ->
            layout.[y] |> Seq.forall ((=) mark)       
        )
        // Check verticals
        [0..2] |> Seq.exists (fun x ->
            [0..2] |> Seq.forall (fun y -> layout.[y].[x] = mark)
        )
        // Check diagonals
        [0..2] |> Seq.forall (fun n -> layout.[n].[n] = mark)
        [0..2] |> Seq.forall (fun n -> layout.[n].[2 - n] = mark)
    ]
    |> Seq.exists (fun x -> x)
    |> Assert.IsTrue