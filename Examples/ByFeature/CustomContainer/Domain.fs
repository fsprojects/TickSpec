module Domain

open System.Diagnostics
open System

type IShelter =
    abstract Available: int
    abstract Bring: int -> unit

// See Autofac delegate factories: http://autofaccn.readthedocs.io/en/latest/advanced/delegate-factories.html
type CatteryFactory = delegate of capacity: int -> Cattery
and Cattery(capacity : int) =
    do Trace.WriteLine("Cattery ctor")
    let mutable available = capacity
    interface IShelter with
        member __.Available = available
        member __.Bring count = available <- available - count
    interface IDisposable with
        member __.Dispose() = Trace.WriteLine("Cattery dtor")

// This type is configured as a Singleton in Autofac to demo that it's created/destroyed once per test run
type DogRun() =
    do Trace.WriteLine("DogRun ctor")
    interface IDisposable with
        member __.Dispose() = Trace.WriteLine "DogRun dtor"

// Depend on DogRun to trigger instantiation
type Kennel(capacity : int, _dogRun: DogRun) =
    do Trace.WriteLine("Kennel ctor")
    let mutable available = capacity
    interface IShelter with
        member __.Available = available
        member __.Bring count = available <- available - count
    interface IDisposable with
        member __.Dispose() = Trace.WriteLine("Kennel dtor")