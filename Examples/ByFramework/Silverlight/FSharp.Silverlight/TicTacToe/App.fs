namespace TicTacToe

open System.Windows

type App() as app =
    inherit Application()    
    do  app.Startup.Add(fun _ -> app.RootVisual <- new Board())