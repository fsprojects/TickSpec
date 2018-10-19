module StockStepDefinitions

open NUnit.Framework
open Retail
open TickSpec
open FSharp.Control.Tasks.ContextInsensitive
open System.Threading.Tasks

let [<Given>] ``a customer buys a black jumper`` () = ()

let [<Given>] ``I have (.*) black jumpers left in stock`` (n:int) = 
    task {
        do! Task.Delay 3000
        return { Count = n }
    }

let [<When>] ``he returns the jumper for a refund`` (stockItem:StockItem) =
    async {
        do! Async.Sleep 1000
        return { stockItem with Count = stockItem.Count + 1 }
    }

let [<Then>] ``I should have (.*) black jumpers in stock`` (n:int) (stockItem:StockItem) =     
    let passed = (stockItem.Count = n)
    Assert.True(passed)