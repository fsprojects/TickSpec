module Domain

open System.Diagnostics
open System

type IShelter =
    abstract Available: int
    abstract Bring: int -> unit

// See Autofac delegate factories: http://autofaccn.readthedocs.io/en/latest/advanced/delegate-factories.html
// While
type CatteryFactory = delegate of capacity: int -> Cattery
and Cattery(capacity : int) =
    do Trace.WriteLine("Cattery ctor")
    let mutable available = capacity
    interface IShelter with
        member __.Available = available
        member __.Bring count = available <- available - count
    interface IDisposable with
        member __.Dispose() = Trace.WriteLine("Cattery- dtor")

type Kennel(capacity : int) =
    do Trace.WriteLine("Kennel ctor")
    let mutable available = capacity
    interface IShelter with
        member __.Available = available
        member __.Bring count = available <- available - count
    interface IDisposable with
        member __.Dispose() = Trace.WriteLine("Kennel dtor")