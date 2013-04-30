// See http://www.infinityfutures.com/documents/ImpliedPriceOverview.pdf
module ImpliedPrice

type ImpliedPriceFixture () = 
    inherit TickSpec.NUnit.FeatureFixture("ImpliedPrice.feature")

open TickSpec
open NUnit.Framework

type Side = Bid | Offer
type Contract = string
type Quantity = int
type Price = int
type Order = Order of Side * Contract * Quantity * Price

let mutable outrightOrders = []

let rec toOrders (table:Table) =
    [for row in table.Rows ->
        match row with
        | [|contract; Int qty; Int price; ""; ""|] -> 
            Order(Bid, contract, qty, price)
        | [|contract; ""; ""; Int price; Int qty|] -> 
            Order(Offer, contract, qty, price)
        | _ -> invalidOp "Unexpected row"
    ]
and (|Int|_|) s = 
    match System.Int32.TryParse(s) with 
    | true, n -> Some n 
    | false,_ -> None 

let [<When>] ``a market place has outright orders:`` (table:Table) =
    outrightOrders <- toOrders table

let [<Then>] ``the market place has synthetic orders:`` (table:Table) =
    let impliedOrders = toOrders table
    ()