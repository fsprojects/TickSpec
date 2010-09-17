namespace TickSpec

type Table (header:string[],rows:string[][]) =        
    new (header) = Table(header,[|[||]|])
    new () = Table([||]) 
    member this.Header = header
    member this.Rows = rows

