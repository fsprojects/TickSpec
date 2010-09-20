module TicTacToeStepDefinitions

open System.Diagnostics
open TickSpec

let mutable layout = [|[||]|]

let [<Given>] ``a board layout:`` (table:Table) =
    layout <- table.Rows         

let [<When>] ``a player marks (X|O) at (top|middle|bottom) (left|middle|right)`` 
        (mark:string,row:string,col:string) =   
    let y =
        match row with
        | "top" -> 0 | "middle" -> 1 | "bottom" -> 2 | s -> invalidOp(s)      
    let x =
        match col with
        | "left" -> 0 | "middle" -> 1 | "right" -> 2 | s -> invalidOp(s)
    Debug.Assert(System.String.IsNullOrEmpty(layout.[y].[x]))
    layout.[y].[x] <- mark      
    
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
        // Check diagnols
        [0..2] |> Seq.forall (fun n -> layout.[n].[n] = mark)
        [0..2] |> Seq.forall (fun n -> layout.[n].[2 - n] = mark)
    ]
    |> Seq.exists (fun x -> x)
    |> Debug.Assert
   
    

