module MorningDewSteps

type MorningDewFixture () = inherit TickSpec.NUnit.FeatureFixture("MorningDew.feature")

open TickSpec
open NUnit.Framework
  
let [<Given>] ``a list of articles:`` (articles:(string * string)[]) = ()

let [<When>] ``read`` () = ()
  
let [<Then>] ``knowledge acquired`` () = ()