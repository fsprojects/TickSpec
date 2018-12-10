module ShoppingSteps

open NUnit.Framework
open TickSpec

type CatalogItem = {Id: int; Description: string; Price: decimal}
type Catalog = Catalog of Season:string * Items:(CatalogItem [])
type Order = Order of string[]
type Receipt = Receipt of string

// Table parameters can be bound to record type arrays
let [<Given>] ``the (.+) product catalog:`` (season: string) (rows: CatalogItem[]) =
    Catalog(season, rows) // Available to later steps

// Bullet list is bound to string[]
let [<When>] ``I make an order:`` (orderItems: string[]) (catalog: Catalog) =
    let receipt =
        """
        Thankyou for your purchase.
          - Black Jumper: $5.00
          - Blue Jeans: $10.00
        Total: $15.00
        """
    (Order orderItems, Receipt receipt) // tuple elements will be injected separately in the next step

// DocString is passed as a regular string following captures
let [<Then>] ``the receipt dated (.+) includes:``
        (receiptDate: string)   // captured
        (expected: string)      // doc string
        (Receipt actual)        // Injected from tuple element
        (catalog: Catalog) =    // Injected from original Step Method

    for line in expected.Split('\n') do
        Assert.True(actual.Contains(line))
