module StockStepDefinitions

open Retail
open TickSpec
open Xunit

let mutable stockItem = { Count = 0 }


type State () =
    let [<Given>] ``a customer buys a black jumper`` () = ()
          
    [<Given>] 
    member __.``I have (.*) black jumpers left in stock`` (n:int) =  
        stockItem <- { stockItem with Count = n }
      
    [<When>] 
    member __.``he returns the jumper for a refund`` () =  
        stockItem <- { stockItem with Count = stockItem.Count + 1 }
      
    [<Then>] 
    member __.``I should have (.*) black jumpers in stock`` (n:int) =     
        let passed = (stockItem.Count = n)
        Assert.True(passed)
    
let mutable blueItem = { Count = 0 }
let mutable blackItem = { Count = 0 }
    
let [<Given>] ``that a customer buys a (black|blue) garment`` 
    (color:string) = 
    () 
    
let [<Given>] ``I have (.*) (black|blue) garments in stock`` 
    (n:int,color:string) =
    match color with
    | "black" -> blackItem <- {blackItem with Count=n}
    | "blue" -> blueItem <- {blueItem with Count=n}    
    | _ -> invalidOp("")

let [<When>] ``he returns the garment for a replacement in (black|blue),`` 
    (color:string) =
    let blueAdd, blackAdd =
        match color with
        | "black" -> 1, -1
        | "blue" -> -1, 1
        | _ -> invalidOp("")
    blackItem <- {blackItem with Count=blackItem.Count+blackAdd} 
    blueItem <- {blueItem with Count=blueItem.Count+blueAdd}    

let [<Then>] ``I should have (.*) (black|blue) garments in stock`` 
    (n:int,color:string) = 
    match color with
    | "black" -> Assert.Equal(blackItem.Count,n)
    | "blue" -> Assert.Equal(blueItem.Count,n)
    | _ -> invalidOp("")