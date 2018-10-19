module HttpServerSteps

open TickSpec

let [<Given>] ``User connects to (.*)`` (server:string) = ()
let [<When>] ``Client requests (.*)`` (page:string) = ()
let [<Then>] ``Server responds with page (.*)`` (page:string) = ()
let [<When>] ``Client sends (.*) to (.*)`` (data:string) (page:string) = ()
let [<Then>] ``Server responds with code (.*)`` (errorcode:int) = ()