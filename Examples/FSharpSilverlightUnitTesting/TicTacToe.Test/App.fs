namespace TicTacToe.Test

open System.Windows
open Microsoft.Silverlight.Testing
open TickSpec

type App() as app =
    inherit Application()    

    do  app.Startup.Add(fun _ ->        
        let features = Assembly.FindFeatures typeof<App>.Assembly
        let settings = UnitTestSettings.Make features
        app.RootVisual <- UnitTestSystem.CreateTestPage settings
    )