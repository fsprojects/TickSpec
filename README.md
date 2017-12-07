[![AppVeyor Build status](https://ci.appveyor.com/api/projects/status/github/fsprojects/tickspec?branch=master&svg=true)](https://ci.appveyor.com/project/sergey-tihon/tickspec/branch/master)
[![NuGet Status](https://img.shields.io/nuget/v/TickSpec.svg?style=flat)](https://www.nuget.org/packages/TickSpec/)
[![Join the chat at https://gitter.im/fsprojects/TickSpec](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsprojects/TickSpec?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Project Description

A lightweight Behaviour Driven Development (BDD) framework.

1. Describe behaviour in plain text using the Gherkin business language, i.e. Given, When, Then.

2. Easily execute the behaviour against matching F# 'ticked' methods, or attribute-tagged C# or F# methods.

3. Run via your normal test runners, set breakpoints in the scenarios and go. Example video: http://www.youtube.com/watch?v=UuTL3nj9fIE

# Installation

Simply reference TickSpec via [Nuget](https://www.nuget.org/packages/TickSpec/), download the assembly or build the project from source.

The TickSpec solution file works in Visual Studio 2015 & 2017, but thebinary should work cleanly on any NET 4.0 or later environment. To run NUnit-based examples, please ensure that you have installed the `NUnit 2 Test Adapter` tool via **Tools|Extensions And Updates**, xUnit.NET examples should work after running `./build.bat`.

Historically, Silverlight was supported; this support and the related examples were removed in 2017 (but remain in the commit history for the archeologically inclined)

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

# Advanced features

## Resolving referenced types (beta)

As it can be seen in the sample [Step definitions (F# without mutable field)](#step-definitions-f-without-mutable-field), the TickSpec framework allows to resolve additional parameters which are not part of the step itself. There are two ways how these parameters are resolved:
* **Preregistered instance:** It is possible to prepare an instance in one of the previous steps. That can be easily achieved by returning the instance from a step. Whenever a step has a return value, then the value is stored under it's type (the step signature is used for the type resolving). There can be only one value for every type. When a parameter is being resolved, it firstly looks into this store of values whether there is not a predefined value.

* **Resolving dependencies:** In case that a type was not already registered in a previous step, then TickSpec finds the widest constructor of such type and tries to create such instance. For that, it is needed to resolve all arguments of the constructor. That is achieved recursively using the same mechanism. The constructed instance is cached, so next time it will return the same instance.

The instances lifetime is per-scenario. So, for every scenario it starts with no created instances and at the end of the scenario it clears all of the instance stores. Moreover, if any instance is IDisposable then the Dispose method is called.

The sample usage is in the example projects DependencyInjection and FunctionalInjection.

## Custom type resolver (beta)

In some cases it may be useful to provide a custom type resolver. That can be achieved by setting the ServiceProviderFactory property of StepDefinitions class. This factory is called on start of every scenario to provide an instance of IServiceProvider implementation. The IServiceProvider implementation is then used for resolving referenced types when an instance was not preregistered (see previous section [Resolving referenced types](#resolving-referenced-types-beta)). If the returned IServiceProvider implementation implements also IDisposable then Dispose is called at the end of the scenario run.

The sample usage is in the example project CustomContainer - it demonstrate wiring of AutoFac including the lifetime scopes per scenario.

# Contributing

Contributions are welcome, particularly examples and documentation. If you have an issue or suggestion please add an Issue. If you'd like to chat about TickSpec please feel free to ping me on [Twitter](http://twitter.com/ptrelford) or go to [the gitter channel](https://gitter.im/fsprojects/TickSpec).
