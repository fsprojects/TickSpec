module Atm

type Account = abstract IsInCredit : bool
type Card = abstract IsValid : bool
type Dispenser = abstract ContainsCash : bool

type Event =
    | ActionRejected
    | AccountDebited
    | CashDispensed
    | CardReturned

let dispenseCash (account:Account, card:Card, dispenser:Dispenser) =
    [
        if not account.IsInCredit
        then yield ActionRejected 
        elif card.IsValid && dispenser.ContainsCash
        then yield! [AccountDebited; CashDispensed]
        yield CardReturned 
    ] 