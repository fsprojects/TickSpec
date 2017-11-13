module Computer

open NUnit.Framework
open TickSpec
open Room

type ComputerState =
    | Off
    | Booting of float
    | Login

let [<When>] ``computer is started in the room`` () =
    Booting 0.0

let [<When>] ``computer runs for (.*) minutes`` (time:float) (state:ComputerState) (RoomTemperature temperature) =
    let newState =
        match state with
        | Booting n ->
            if (n + time > 8.0) then Login else Booting (n + time)
        | _ -> state
    let newTemp = RoomTemperature (temperature + time/10.0)
    newState, newTemp

let [<Then>] ``computer is on login screen`` (state:ComputerState) =
    Assert.AreEqual(Login, state)