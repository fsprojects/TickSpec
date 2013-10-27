namespace TicTacToe

open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Media
open System.Windows.Shapes
open Wiggle

type Board () as this =
    inherit UserControl ()
    let width, height = 800.0, 600.0
    let markSize = height/8.0
    do this.Width <- width; this.Height <- height
    let panel = Grid(Background=SolidColorBrush(Colors.Transparent))
    let canvas = Canvas()
    do  panel.Children.Add canvas
        this.Content <- panel
    let add (x:#UIElement) = canvas.Children.Add x
    do for i = 1 to 2 do
        let x = float i * width / 3.0
        let y = float i * height / 3.0 
        add (CreateLine(Point(0.0,y),Point(width,y)))
        add (CreateLine(Point(x,0.0),Point(x,height)))
    let setPosition (graphic,x,y) =
        Canvas.SetLeft(graphic,x)
        Canvas.SetTop(graphic,y)
    let createX () =
        let canvas = Canvas()
        [CreateLine(Point(-markSize,-markSize),Point(markSize,markSize))
         CreateLine(Point(markSize,-markSize),Point(-markSize,markSize))]
        |> List.iter canvas.Children.Add
        canvas
    let createO () = 
        let canvas = Canvas()
        CreateEllipse(Point(0.0,0.0),markSize,markSize)
        |> canvas.Children.Add
        canvas
    let mutable markFun = createX
    let cursor = ref (markFun ())
    let initCursor () =
        (!cursor).Opacity <- 0.5
        setPosition(!cursor,-99.9,-99.9)
        add !cursor
    do  initCursor ()
    let ToPoint (col,row) =
        ((float col * width) / 3.0) + (width / 6.0),
        ((float row * height) / 3.0) + (height / 6.0)
    let subscriptions =
        [panel.MouseMove.Subscribe (fun (me:MouseEventArgs) ->
            let point = me.GetPosition(panel)
            setPosition(!cursor,point.X,point.Y)
         )
         panel.MouseLeftButtonDown.Subscribe (fun (me:MouseButtonEventArgs) ->
            let point = me.GetPosition(panel)
            let col, row = int (point.X*3.0/width), int (point.Y*3.0/height)
            if board.[col,row].IsNone then
                board.[col,row] <- Some mark
                let x,y = ToPoint(col,row)
                let graphic = markFun ()
                setPosition(graphic,x,y)
                graphic |> add
                canvas.Children.Remove(!cursor) |> ignore
                match winningLine() with
                | Some line ->
                    let col1,row1 = line.Item(0)
                    let col2,row2 = line.Item(2)
                    let x1,y1 = ToPoint(col1,row1)
                    let x2,y2 = ToPoint(col2,row2)
                    let cross = CreateLine(Point(x1,y1),Point(x2,y2))
                    cross |> add
                | None ->
                    alternateMark()
                    markFun <- if mark = O then createO else createX
                    cursor := markFun ()
                    initCursor ()
         )]
    interface System.IDisposable with
        member this.Dispose() = 
            subscriptions 
            |> List.iter (fun disposable -> disposable.Dispose())