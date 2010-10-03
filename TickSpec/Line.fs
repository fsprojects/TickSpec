namespace TickSpec

open System

type internal Line =
    {        
        Number : int
        Text : string        
        Bullets : string[] option
        Table : Table option
    }
