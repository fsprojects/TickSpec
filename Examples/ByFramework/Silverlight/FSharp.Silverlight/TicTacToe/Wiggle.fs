namespace TicTacToe

open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Shapes

type Vector(x:double,y:double) =
    member this.X = x
    member this.Y = y
    member this.Length = sqrt(x * x + y * y)

module Straight =
    let CreateLine(point1:Point,point2:Point) =
        let line = Line(Stroke=SolidColorBrush(Colors.Black),StrokeThickness=6.0)
        line.X1 <- point1.X
        line.Y1 <- point1.Y
        line.X2 <- point2.X
        line.Y2 <- point2.Y
        line
    let CreateEllipse(center:Point,radiusX,radiusY) =
        let ellipse = Ellipse(Stroke=SolidColorBrush(Colors.Black),StrokeThickness=6.0)
        ellipse.Width <- radiusX
        ellipse.Height <- radiusY
        ellipse

module Wiggle =
    let internal rand = System.Random()
    let internal Wiggle () = rand.NextDouble() * 3.0 - 1.5
    let CreateLine(point1:Point,point2:Point) =
        let polyline = 
            Polyline(Stroke=SolidColorBrush(Colors.Black),StrokeThickness=6.0)
        let vector = Vector(point2.X - point1.X, point2.Y - point1.Y)
        let segmentLength = 10.0
        let count = int (vector.Length/segmentLength) + 1
        for i = 0 to count-1 do
            let x = point1.X + (vector.X * float i) / float count + Wiggle()
            let y = point1.Y + (vector.Y * float i) / float count + Wiggle()
            polyline.Points.Add(Point(x,y))
        polyline
    let CreateEllipse(center:Point,radiusX,radiusY) =
        let polygon =
            Polygon(Stroke=SolidColorBrush(Colors.Black),StrokeThickness=6.0)
        let a' = rand.Next() * 360
        for a in 0..10..360 do 
            let r = float ((a+a') % 360) * System.Math.PI / 180.0
            let x = center.X + radiusX * cos(r) + Wiggle()
            let y = center.Y + radiusY * sin(r) + Wiggle()
            polygon.Points.Add(Point(x,y))
        polygon