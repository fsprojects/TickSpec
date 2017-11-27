module ShelterSteps

open Domain
open TickSpec
open Swensen.Unquote
open System

/// Two simple examples of factory methods Autofac can generate
/// See Autofac delegate factories: http://autofaccn.readthedocs.io/en/latest/advanced/delegate-factories.html
type BootstrappingSteps(createCattery : CatteryFactory, createKennel : Func<int,Kennel>) =
    [<Given>]
    member __.``a cattery with (\d+) spaces`` count =
        createCattery.Invoke count
    [<Given>]
    member __.``a kennel with (\d+) spaces`` count =
        createKennel.Invoke count

let [<When>] ``I bring (\d+) dogs`` count (kennel : Kennel) =
    let shelter : IShelter = kennel :> _
    shelter.Bring count
let [<When>] ``I bring (\d+) cats`` count (cattery : Cattery) =
    let shelter : IShelter = cattery :> _
    shelter.Bring count

let [<Then>] ``(\d+) sheltering slots remain`` count (cattery : Cattery, kennel : Kennel) =
    let shelters = [cattery :> IShelter; kennel :> IShelter]
    test <@ count = List.sum [for s in shelters -> s.Available] @>