module BulletTime

type BulletTimeFixture () = inherit TickSpec.NUnit.FeatureFixture("BulletTime.feature")

open TickSpec
open NUnit.Framework
open Microsoft.FSharp.Collections

let mutable availableActors = Set.empty<string>

let [<Given>] ``the following actors:`` (actors : string[]) =
    availableActors <- Set.ofArray actors

let [<When>] ``the following are not available:`` (unavailableActors : string[]) =
    availableActors <- availableActors - (Set.ofArray unavailableActors)

let [<Then>] ``(.+) is the obvious choice`` (actor : string) =
    Assert.AreEqual(actor, Seq.exactlyOne availableActors)