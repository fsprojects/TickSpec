module TicTacToeStepDefinitions

open System.Diagnostics
open TickSpec
open NUnit.Framework

let mutable layout = [|[||]|]

let [<Given>] ``a board layout:`` (table:Table) =
    layout <- table.Rows         

type Row = top = 0 | middle = 1 | bottom = 2
type Col = left = 0 | middle = 1 | right = 2
let [<Literal>] rowEx = "(top|middle|bottom)"
let [<Literal>] colEx = "(left|middle|right)"

[<When("a player marks (X|O) at {0} {1}", rowEx, colEx)>]
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
    |> Assert.IsTrue
    

