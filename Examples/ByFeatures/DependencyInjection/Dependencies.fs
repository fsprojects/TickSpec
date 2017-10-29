module Dependencies

type IDependency =
    abstract member Value : string with get, set

type FirstDependencyImplementation () =
    let mutable _value = ""

    interface IDependency with
        member this.Value
            with get() = _value
            and set(value) = _value <- (sprintf "First: %s" value)

type SecondDependencyImplementation () =
    let mutable _value = ""

    interface IDependency with
        member this.Value
            with get() = _value
            and set(value) = _value <- (sprintf "Second: %s" value)