namespace Computation

type Calculator() =
    let mutable values : int list = []

    member __.Push(n) = values <- n :: values
    member __.Add() = values <- [List.sum values]
    member __.Result = values.Head