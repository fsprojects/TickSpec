// Valet Parking sample based on Chapter 3, Page 42 of ATDD by Example by Markus Gärtner,
// http://www.informit.com/store/atdd-by-example-a-practical-guide-to-acceptance-test-9780321784155
module ValetParkingSteps

type ImpliedPriceFixture () = 
    inherit TickSpec.NUnit.FeatureFixture("ValetParking.feature")

open System

let calculateCost (days, hours, minutes) =
    let halfHours = 2 * hours + minutes/30
    match days, halfHours with
    | 0, n when n < 2 -> 2.0M
    | d, n when n <= 24 -> decimal (d*24 + n)
    | d, n -> decimal ((d+1) * 24) 

let (|Match|_|) pattern input =
    let m = System.Text.RegularExpressions.Regex.Match(input, pattern)
    if m.Success then Some (m.Groups.[1].Value, input.Substring(m.Value.Length).Trim()) else None

let (|Duration|) s =
    let rec parse acc s =
        let (+) (d,h,m) (d',h',m') = (d+d',h+h',m+m') 
        match s with
        | Match @"^(\d+)\s+day" (n,s) -> s, (Int32.Parse(n),0,0)
        | Match @"^(\d+)\s+hours" (n,s) -> s, (0,Int32.Parse(n),0)
        | Match @"^(\d+)\s+hour" (n,s) -> s, (0,Int32.Parse(n),0)
        | Match @"^(\d+)\s+minutes" (n,s) -> s, (0,0,Int32.Parse(n))
        | _ -> invalidOp s
        |> function
        | "", t -> t + acc
        | s, t -> parse (t + acc) s
    parse (0,0,0) s
    
let (|Cost|) (s:string) = s.Trim('$').Trim() |> Decimal.Parse

open NUnit.Framework
open TickSpec

let mutable calculatedCost = 0M
let [<When>] ``I park my car in the Valet Parking Lot for (.*)`` (Duration duration) =
    calculatedCost <- calculateCost duration 
let [<Then>] ``I will have to pay (.*)`` (Cost cost) =
    Assert.AreEqual(cost, calculatedCost)