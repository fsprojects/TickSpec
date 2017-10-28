module ShoppingSteps

open TickSpec
open NUnit.Framework

//todo: our goal of functional injection is to get rid of need of this mutable field
let mutable wallet = 0

let [<Given>] ``I have (.*) dollars in my wallet`` amount =
    wallet <- amount

let [<When>] ``I buy an item for (.*) dollars`` cost =
    wallet <- wallet - cost

let [<Then>] ``My wallet contains (.*) dollars`` amount =
    Assert.AreEqual(wallet, amount)

