module StockStepDefinitions

type StockItem = { Count : int }

open NUnit.Framework
open TickSpec

let [<Given>] ``a customer buys a black jumper`` () = ()

let [<Given>] ``I have (.*) black jumpers left in stock`` (n:int) =  
    { Count = n }

let [<When>] ``he returns the jumper for a refund`` (stockItem:StockItem) =  
    { stockItem with Count = stockItem.Count + 1 }

let [<Then>] ``I should have (.*) black jumpers in stock`` (n:int) (stockItem:StockItem) =     
    let passed = (stockItem.Count = n)
    Assert.True(passed)