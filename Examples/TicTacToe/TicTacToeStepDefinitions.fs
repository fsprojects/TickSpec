module TicTacToeStepDefinitions

open System.Diagnostics
open TickSpec

let mutable layout = [|[||]|]

let [<Given>] ``a board layout:`` (table:Table) =
    layout <- table.Rows         

type Row = Top = 0 | Middle = 1 | Bottom = 2
type Col = Left = 0 | Middle = 1 | Right = 2

let [<Parser>] toCol = function
    | "left" -> Col.Left | "middle" -> Col.Middle | "right" -> Col.Right
    | _ -> raise (new System.InvalidCastException())

let [<Parser>] toRow = function
    | "top" -> Row.Top | "middle" -> Row.Middle | "bottom" -> Row.Bottom
    | _ -> raise (new System.InvalidCastException())
   
let [<When>] ``a player marks (X|O) at (top|middle|bottom) (left|middle|right)`` 
        (mark:string,row:Row,col:Col) =       
    let y = int row             
    let x = int col        
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
        // Check diagonals
        [0..2] |> Seq.forall (fun n -> layout.[n].[n] = mark)
        [0..2] |> Seq.forall (fun n -> layout.[n].[2 - n] = mark)
    ]
    |> Seq.exists (fun x -> x)
    |> Debug.Assert
   
    

