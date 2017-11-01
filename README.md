# Project Description

A lightweight Behaviour Driven Development (BDD) framework.

Describe behaviour in plain text using the Gherkin business language, i.e. Given, When, Then.

Easily execute the behaviour against matching F# tick methods, attributed C# or F# methods.

    let ``tick method`` () = true

# Installation

Simply reference TickSpec via [Nuget](https://www.nuget.org/packages/TickSpec/), download the assembly or build the project from source.
TickSpec works in Visual Studio 2015 & 2017.
Historically, Silverlight was supported; this support and the related examples were removed in 2017 (but remain in the commit history for the archeologically inclined)

Example video: http://www.youtube.com/watch?v=UuTL3nj9fIE

# Feature specification (Plain text)

```
Feature: Refunded or replaced items should be returned to stock

Scenario 1: Refunded items should be returned to stock
    Given a customer buys a black jumper
    And I have 3 black jumpers left in stock 
    When he returns the jumper for a refund 
    Then I should have 4 black jumpers in stock 
```

# Step definitions (F#)

```
let mutable stockItem = { Count = 0 }

let [<Given>] ``a customer buys a black jumper`` () = ()
      
let [<Given>] ``I have (.*) black jumpers left in stock`` (n:int) =  
    stockItem <- { stockItem with Count = n }
      
let [<When>] ``he returns the jumper for a refund`` () =  
    stockItem <- { stockItem with Count = stockItem.Count + 1 }
      
let [<Then>] ``I should have (.*) black jumpers in stock`` (n:int) =     
    let passed = (stockItem.Count = n)
    Debug.Assert(passed)
```

# Step definitions (C#)

```
public class StockStepDefinitions
{
   private StockItem _stockItem;

   [Given(@"a customer buys a black jumper")]
   public void GivenACustomerBuysABlackJumper()
   {
   }

   [Given(@"I have (.*) black jumpers left in stock")]
   public void GivenIHaveNBlackJumpersLeftInStock(int n)
   {
      _stockItem = new StockItem() { Count = n };  
   }

   [When(@"he returns the jumper for a refund")]
   public void WhenHeReturnsTheJumperForARefund()
   {
      _stockItem.Count += 1;
   }

   [Then(@"I should have (.*) black jumpers in stock")]
   public void ThenIShouldHaveNBlackJumpersInStock(int n)
   {
      Debug.Assert(_stockItem.Count == n);
   }
}
```

# Contributing

Contributions are welcome, particularly examples and documentation. If you have an issue or suggestion please add an Issue. If you'd like to chat about TickSpec please feel free to ping me on [Twitter](http://twitter.com/ptrelford).

TickSpec builds [continuously](http://teamcity.codebetter.com/project.html?projectId=project410) with [TeamCity](http://www.jetbrains.com/teamcity/) on [CodeBetter's CI Server](http://codebetter.com/kylebaley/2010/02/11/codebetter-ci-server-update-or-how-to-plead-your-case/).
