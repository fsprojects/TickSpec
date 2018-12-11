module WalletSteps

open NUnit.Framework
open TickSpec

type Wallet = WalletDollars of int

let [<Given>] ``I have (.*) dollars in my wallet`` (amount:int) =
    WalletDollars amount // The output of function gets stored by type

// Extra parameters are restored from store by its type, in this case it is the wallet
let [<When>] ``I buy an item for (.*) dollars`` cost (WalletDollars wallet) =
    // Wallet value is replaced in store thanks to this output
    WalletDollars (wallet - cost)

let [<Then>] ``My wallet contains (.*) dollars`` (amount:int) (WalletDollars wallet) =
    Assert.AreEqual(amount, wallet)