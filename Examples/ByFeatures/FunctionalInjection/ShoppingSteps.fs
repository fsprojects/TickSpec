module ShoppingSteps

open TickSpec
open NUnit.Framework

type Wallet = WalletDollars of int

let [<Given>] ``I have (.*) dollars in my wallet`` (amount:int) =
    WalletDollars amount // The output of function gets stored by type

let [<When>] ``I buy an item for (.*) dollars`` cost (WalletDollars wallet) = // Extra parameters are restored from store by its type, in this case it is the wallet 
    WalletDollars (wallet - cost) // Wallet value is replaced in store thanks to this output

let [<Then>] ``My wallet contains (.*) dollars`` (amount:int) (WalletDollars wallet) =
    Assert.AreEqual(amount, wallet)

