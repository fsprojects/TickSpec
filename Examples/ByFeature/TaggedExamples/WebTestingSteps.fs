module WebTestingSteps

open TickSpec
open NUnit.Framework

let [<Given>] ``I use browser (.*)`` (browser:string) = ()
let [<When>] ``I try to login using username (.*) and password (.*)`` (username:string) (password:string) = ()
let [<When>] ``I go to the main page`` () = ()
let [<Then>] ``I am logged in`` () = ()
let [<Then>] ``The main page is displayed`` () = ()