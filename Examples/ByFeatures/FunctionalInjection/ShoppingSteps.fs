﻿module ShoppingSteps

open TickSpec
open NUnit.Framework

let [<Given>] ``I have (.*) dollars in my wallet`` amount =
    amount

let [<When>] ``I buy an item for (.*) dollars`` cost wallet =
    wallet - cost

let [<Then>] ``My wallet contains (.*) dollars`` (amount:int) (wallet:int) =
    Assert.AreEqual(wallet, amount)
