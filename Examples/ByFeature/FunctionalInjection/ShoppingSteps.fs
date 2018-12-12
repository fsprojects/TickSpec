module ShoppingSteps

open System
open NUnit.Framework
open TickSpec

type CatalogItem = {Id: int; Description: string; Price: decimal}
type Year = Year of int
type Catalog = Catalog of Year:Year * Items:(CatalogItem [])
type Catalogs = Map<Year, Catalog>
type Receipt = Receipt of Date:DateTime * Lines:(string [])

let [<Given>] ``no catalogs prior to 2018``() =
    Map.empty: Catalogs

// Table parameters can be bound to record type arrays
let [<Given>] ``the (.+) product catalog:`` (yearInt: int) (items: CatalogItem[]) (catalogs: Catalogs) =
    let year = Year yearInt
    let catalog = Catalog(year, items)
    Map.add year catalog catalogs

// Bullet list is bound to string[]
let [<When>] ``I make a purchase on (.*):`` (purchaseDate: DateTime) (orderItems: string[]) (catalogs: Catalogs) =
    let Catalog(Items=catalogItems) = catalogs |> Map.find (Year purchaseDate.Year)
    let receiptLines = [|
        for orderItem in orderItems do
            let catalogItem = Array.find (fun x -> x.Description = orderItem) catalogItems
            yield sprintf "%s: $%.2f" catalogItem.Description catalogItem.Price
    |]
    Receipt(purchaseDate, receiptLines)

// DocString is passed as a regular string following captures
let [<Then>] ``the receipt dated (.+) includes:`` (expectedDate: DateTime) (expected: string) (receipt: Receipt) =
    let expectedLines = expected.Split('\n') |> Array.map(fun s -> s.Trim())
    let Receipt(Date=receiptDate; Lines=receiptLines) = receipt
    Assert.AreEqual(expectedDate, receiptDate)
    Assert.AreEqual(expectedLines, receiptLines)