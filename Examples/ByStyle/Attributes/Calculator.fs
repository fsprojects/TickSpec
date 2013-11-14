namespace Library

/// Calculator type
type Calculator () =
    let mutable values = []
    let mutable total = 0
    /// Pushes value onto values list
    member this.Push value = values <- value :: values
    member this.Sum() = 
        total <- List.sum values
        values <- []
    member this.Total = total

