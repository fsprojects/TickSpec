[![Build Status](https://github.com/fsprojects/TickSpec/workflows/Build%20and%20Test/badge.svg?branch=master)](https://github.com/fsprojects/TickSpec/actions?query=branch%3Amaster)
[![NuGet Status](https://img.shields.io/nuget/v/TickSpec.svg?style=flat)](https://www.nuget.org/packages/TickSpec/)
[![Join the chat at https://gitter.im/fsprojects/TickSpec](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/fsprojects/TickSpec?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

# Project Description

A lightweight Behaviour Driven Development (BDD) framework for .NET that'll fit how you want to test.

1. Describe behaviour in plain text using the Gherkin business language, i.e. Given, When, Then.
2. Easily execute the behaviour against matching F# 'ticked' methods, or attribute-tagged C# or F# methods.
3. Run via your normal test runners or plugins (xUnit, NUnit or standalone)
4. Set breakpoints in the scenarios, step definitions or your code and go (setting breakpoints in the Gherkin is currently not supported in .NET Standard version)

Example video: http://www.youtube.com/watch?v=UuTL3nj9fIE

# Installation

Simply reference TickSpec via [NuGet or Paket](https://www.nuget.org/packages/TickSpec/), download the assembly or build the project from source.

- The binary should work cleanly on any .NET Standard 2.0, .NET 4.5 or later environment.
- The TickSpec solution file works with Visual Studio 2017. 
- Historically, Silverlight was supported; this support and the related examples were removed in 2017 (but remain in the commit history for the archeologically inclined)
- Useful samples are available in the Examples folder https://github.com/fsprojects/TickSpec/tree/master/Examples/ and depending your choice, this one is a good start : https://github.com/fsprojects/TickSpec/tree/master/Examples/ByFramework 

# Feature specification (Plain text)

```Gherkin
Feature: Refunded or replaced items should be returned to stock

Scenario: Refunded items should be returned to stock
    Given a customer buys a black jumper
    And I have 3 black jumpers left in stock
    When he returns the jumper for a refund
    Then I should have 4 black jumpers in stock
```

# Step definitions (F#)

```FSharp
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

```FSharp
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

```CSharp
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

# Type Conversions

Arguments to Step Methods will be converted from `string` to the declared types of the Step Method parameters when possible.
The following conversions are supported:

 - `Enum` types
 - `Union` types with no parameters
 - `Nullable<T>` types where the inner `T` type can be converted from `string`
 - `Tuple` types where each element can be converted from `string`
 - Array types `T []` where `T` can be converted from `string` and the original `string` is comma delimited
 - Types supported by `System.Convert.ChangeType`

# Tables

A table may be passed as an argument to a Step Method:

```gherkin
When a market place has outright orders:
   | Contract | Bid Qty | Bid Price | Offer Price | Offer Qty |
   | V1       | 1       | 9505      |             |           |
   | V2       |         |           | 9503        | 1         |
```

The parameter can be declared with type `Table`:

```fsharp
let [<When>] ``a market place has outright orders:`` (table:Table) =
    outrightOrders <- toOrders table
```

Alternatively, the parameter can be converted to an array of records, or other type with constructor parameters supported by the [Type Conversions](#type-conversions)

```fsharp
type OrderRow = { Contract: string; BidQty: string; BidPrice: string; OfferPrice: string; OfferQty: string }

let [<When>] ``a market place has outright orders:`` (orderRows: OrderRow[]) =
    outrightOrders <- toOrders orderRows
```

The `Table` parameter must appear after any regex capture parameters, and before any `Functional Injection` parameters:

```fsharp
let [<Given>] ``A market place``() =
    createMarketPlace()

let [<When>] ``a market place has outright (.*) orders:``
    (orderType: string)       # captured
    (table: Table)            # table
    (maketPlace: MarketPlace) # injected
    =
  ...
```

# Lists

A bullet list may be passed to a Step Method similarly to a [Table](#tables):

```gherkin
Scenario: Who is the one?
Given the following actors:
    * Keanu Reeves
    * Bruce Willis
    * Johnny Depp
When the following are not available:
    * Johnny Depp
    * Bruce Willis
Then Keanu Reeves is the obvious choice
```

The parameter type must be an array type supported by the [Type Conversions](#type-conversions):

```fsharp
let [<Given>] ``the following actors:`` (actors : string[]) =
    availableActors <- Set.ofArray actors
```

# Advanced features

## Resolving referenced types (beta)

As shown in [Step definitions (F# without mutable field)](#step-definitions-f-without-mutable-field), TickSpec also allows one to request additional parameters along with the captures from the regex holes in the step name as per typical Gherkin based frameworks. Such additional parameters can be fulfilled via the following mechanisms:
* **Instances returned from Step Method return values:** This involves generating and stashing an _instance_ in one of the preceding steps in the scenario. Typically this is achieved by returning the instance from the Step Method. Whenever a step has a return value, the the value is saved under it's type (the return type of the Step Method controls what the type is). Multiple values can be returned from a Step Method by returning a tuple. There can be only one value per type stashed per scenario run. When a parameter is being resolved, TickSpec first attempts to resolve from this type-to-instance caching Dictionary.

* **Resolving dependencies:** If an instance cannot be located in the type-to-instance cache based on a preceding step having stashed the value, TickSpec will attempt to use the 'widest' constructor (the one with the most arguments) of the required type to instantiate it. Any input arguments to the constructor are all resolved recursively using the same mechanism. Any constructed instances are also cached in the type-to-instance cache, so next time it will return the same instance.

* **Accessing scenario metadata:** You can access contextual information about the scenario within which a step definition is executing (e.g. tags). To do this, add a parameter of type  `ScenarioMetadata` in the step definition argument list (or in a constructor parameter list of a dependency)
scenario information (e.g. tags), then you can reference `ScenarioMetadata` in your method argument (or in constructor of dependencies) and you will get an instance describing the scenario which is invoked.

The lifetime of instances is per-scenario:- Each scenario run starts an empty type-to-instance cache, and at the end of the scenario the cache gets cleared. Moreover, if any instance is `IDisposable`, `Dispose` will be called.

See the example projects `DependencyInjection` and `FunctionalInjection` for typical and advanced examples of using this mechanism.

## Custom type resolver (beta)

While the typical recommended usage of TickSpec is to keep the step definitions simple and drive a system from the outside in the simplest fashion possible, in some advanced cases it may be useful to provide a custom type resolver. This can be achieved by `set`ting the `StepDefinitions.ServiceProviderFactory` property. This factory method is used once per scenario run to establish an independent resolution context per scenario run. The `IServiceProvider` instance is used to replace the built in instance construction mechanism (see _Resolving dependencies_ in the previous section: [Resolving referenced types](#resolving-referenced-types-beta)). If the `IServiceProvider` implementation yielded by the factory also implements `IDisposable`, `Dispose` is called on the Service Provider context at the end of the scenario run.

See the `CustomContainer` example project for usage examples - the example demonstrates wiring of Autofac including usage of lifetime scopes per scenario and usage of the [xUnit 2+ Shared Fixtures](https://xunit.github.io/docs/shared-context.html) to correctly manage the sharing/ifetime of the container where one runs xUnit Test Classes in parallel as part of a large test suite.

# Build integration

TickSpec.Build automatically integrates into the build process as "BeforeCompile" target and generates the "wiring" needed to execute 
the BDD scenarios. Currently only NUnit is supported as test framework.

## HTML documentation

TickSpec.Build additionally supports generating HTML documents for the feature files

```bash
TickSpec.Build doc ./src ./html
```

When generating the HTML files to the output location the F# project local folders are preserved.

Using ``--toc html`` a HTML table of contents and with ``--toc json`` a Json table of contents can be generated.

### Styling

The generated HTML documents intentionally only contain HTML fragments of type "article" so that those 
articles can easily be integrated in an existing HTML documentation. 

These articles provide the following CSS classes for styling:

- **gherkin-keyword** applies to the keywords like GIVEN, WHEN, THEN
- **gherkin-scenario-body** applies to the body of a scenario
- **gherkin-scenario** applies to a complete scenario
- **gherkin-tags** applies to the tags attached to scenarios
- **gherkin-description** applies to a comment provided above a scenario
- **gherkin-scenario-title** applies to the title of a scenario
- **gherkin-feature-title** applies to the feature title

If you want to use the generated articles as a standalone documentation use ``--toc html`` to generate a 
standalone HTML document. Put a ``style.css`` next to the ``ToC.html`` to define the CSS classes listed above

### MsBuild integration

To integrate the HTML generation into your MsBuild based build process set the property ``FeatureFileHtmlOutput``
to the location the HTML files should be generated too. By default, only the feature files local to this project
are considered. You can change this by setting the property ``FeatureFileHtmlInput``.

The format of the table of contents can be set using property ``TickSpecBuildTocFormat``.


## Story behind this project

The following articles tell the story behind this project:

- [Lean BDD and Code Generation](http://www.plainionist.net/TickSpec-with-Code-Generation/)
- [Lean BDD with even more Code Generation](http://www.plainionist.net/TickSpec-More-CodeGen/)


# Contributing

Contributions are welcome, particularly examples and documentation. If you'd like to chat about TickSpec, please use the [the gitter channel](https://gitter.im/fsprojects/TickSpec).

For issues or suggestions please raise an Issue. If you are looking to extend or change the core implementation, it's best to drop a quick note and/or a placeholder PR in order to make sure there is broad agreement on the scope of the change / nature of the feature in question before investing significant time on it; we want to keep TickSpec _powerful, but minimal_.
