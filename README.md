[![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/github/fsprojects/tickspec?branch=master&svg=true)](https://ci.appveyor.com/project/sergey-tihon/tickspec/branch/master)
[![NuGet Status](https://img.shields.io/nuget/v/TickSpec.svg?style=flat)](https://www.nuget.org/packages/TickSpec/)
[![Join the chat at https://gitter.im/fsprojects/TickSpec](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsprojects/TickSpec?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Project Description

A lightweight Behaviour Driven Development (BDD) framework.

Describe behaviour in plain text using the Gherkin business language, i.e. Given, When, Then.

Easily execute the behaviour against matching F# tick methods, attributed C# or F# methods.

    let ``tick method`` () = true

# Installation

Simply reference TickSpec via [Nuget](https://www.nuget.org/packages/TickSpec/), download the assembly or build the project from source.
TickSpec works in Visual Studio 2015 & 2017.
To run NUnit based examples, please ensure that toy have installed the `NUnit 2 Test Adapter` tool via **Tools|Extensions And Updates**.
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
type StockItem = { Count : int }

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

# Step definitions (F# without mutable field)

```
type StockItem = { Count : int }

let [<Given>] ``a customer buys a black jumper`` () = ()
      
let [<Given>] ``I have (.*) black jumpers left in stock`` (n:int) =
    { Count = n }
      
let [<When>] ``he returns the jumper for a refund`` (stockItem:StockItem) =
    { stockItem with Count = stockItem.Count + 1 }
      
let [<Then>] ``I should have (.*) black jumpers in stock`` (n:int) (stockItem:StockItem) =
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

Contributions are welcome, particularly examples and documentation. If you have an issue or suggestion please add an Issue. If you'd like to chat about TickSpec please feel free to ping me on [Twitter](http://twitter.com/ptrelford) or go to [the gitter channel](https://gitter.im/fsprojects/TickSpec).
