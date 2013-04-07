module AtmSteps

type AccountFixture () = inherit TickSpec.NUnit.FeatureFixture("Atm.feature")

open Atm
open TickSpec
open NUnit.Framework

type Property =
    | AccountIsInCredit
    | CardIsValid
    | DispenserContainsCash
    override property.ToString() = Union.CaseName(property)

let mutable properties = []
let mutable events = []
let hold x = properties <- x::properties 
let holds x = properties |> List.exists ((=) x)

let [<BeforeScenario>] Setup () =
    properties <- []
    events <- []

let [<Given>] ``the account is in credit`` () = 
    hold AccountIsInCredit

let [<Given>] ``the account is overdrawn`` () = 
    ()

let [<Given>] ``the card is valid`` () =
    hold CardIsValid

let [<Given>] ``the dispenser contains cash`` () =
    hold DispenserContainsCash

let [<When>] ``the customer requests cash`` () =
    let account = 
        { new Account with 
           member x.IsInCredit = holds AccountIsInCredit }
    let card = 
        { new Card with 
           member x.IsValid = holds CardIsValid }
    let dispenser = 
        { new Dispenser with 
           member x.ContainsCash = holds DispenserContainsCash }
    events <- dispenseCash (account, card, dispenser)

let [<Then>] ``ensure the account is debited``() =
    Assert.Contains(AccountDebited, events)

let [<Then>] ``ensure a rejection message is displayed`` () =
    Assert.Contains(ActionRejected, events)

let [<Then>] ``ensure cash is dispensed`` () =
    Assert.Contains(CashDispensed, events)

let [<Then>] ``ensure cash is not dispensed`` () =
    events |> List.exists ((=) CashDispensed) |> Assert.IsFalse

let [<Then>] ``ensure the card is returned`` () =
    Assert.Contains(CardReturned, events)