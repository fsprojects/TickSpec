namespace TickSpec

module internal List =
    /// Computes all combinations
    let rec combinations = function
    | [] -> [[]]
    | hs :: tss ->
        [ for h in hs do
            for ts in combinations tss ->
                h :: ts ]

module internal Seq =
    /// Skips elements in sequence until condition is met  
    let skipUntil (p:'a -> bool) (source:seq<_>) =  
        seq { 
            use e = source.GetEnumerator() 
            let latest = ref (Unchecked.defaultof<_>)
            let ok = ref false
            while e.MoveNext() do
                if (latest := e.Current; (!ok || p !latest)) then
                    ok := true
                    yield !latest 
        }
        
 module internal TextReader =
    /// Reads lines from TextReader
    let readLines (reader:System.IO.TextReader) =
        seq {
            let isEOF = ref false
            while not !isEOF do
                let line = reader.ReadLine()
                if line <> null then yield line
                else isEOF := true
        }
    /// Read all lines to a string array
    let readAllLines reader = reader |> readLines |> Seq.toArray    

module internal Dict =
    let ofSeq (pairs:('key * 'value) seq) =
        let dict = System.Collections.Generic.Dictionary()
        pairs |> Seq.iter (fun (key,value) -> dict.Add(key,value))
        dict