module TickSepc.Build.Tests.HtmlGenerationTests

open NUnit.Framework
open TickSpec.Build
open FsUnit

[<Test>]
let ``Feature title is headline``() =
    """
    Feature: First feature

    Scenario: One
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """<h2 class="gherkin-feature-title">First feature</h2>"""

[<Test>]
let ``Scenario title is headline``() =
    """
    Feature: First feature

    Scenario: One
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """<h3 class="gherkin-scenario-title">One</h3>"""
        
[<Test>]
let ``Scenario Outline title is headline``() =
    """
    Feature: First feature

    Scenario Outline: One
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """<h3 class="gherkin-scenario-title">One</h3>"""
        
[<Test>]
let ``Steps rendered as <code/>``() =
    """
    Feature: First feature

    Scenario: One
    GIVEN some environment
     AND with following setting
    WHEN some event happens
    THEN the system should be in this state
    AND behave like this
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario"><h3 class="gherkin-scenario-title">One</h3>
      <pre class="gherkin-scenario-body"><code><span class="gherkin-keyword">Given</span> some environment
<span class="gherkin-keyword">And</span> with following setting
<span class="gherkin-keyword">When</span> some event happens
<span class="gherkin-keyword">Then</span> the system should be in this state
<span class="gherkin-keyword">And</span> behave like this
</code></pre></div>"""
        
[<Test>]
let ``With Background``() =
    """
    Feature: First feature

    Background:
        GIVEN some additional environment

    Scenario: One
    GIVEN some environment
     AND with following setting
    WHEN some event happens
    THEN the system should be in this state
    AND behave like this
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
        <article>
          <h2 class="gherkin-feature-title">First feature</h2>
          <div class="gherkin-scenario">
            <h3 class="gherkin-scenario-title">Background</h3>
            <pre class="gherkin-scenario-body"><code><span class="gherkin-keyword">Given</span> some additional environment
</code></pre></div>"""

[<Test>]
let ``Step with multi line string``() =
    """
    Feature: First feature

    Scenario: One
    GIVEN some environment
     AND the following value
        \"\"\"
        line 1
        line 2
        \"\"\"
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario"><h3 class="gherkin-scenario-title">One</h3>
      <pre class="gherkin-scenario-body"><code><span class="gherkin-keyword">Given</span> some environment
<span class="gherkin-keyword">And</span> the following value
    \"\"\"
    line 1
    line 2
    \"\"\"
<span class="gherkin-keyword">When</span> some event happens
<span class="gherkin-keyword">Then</span> the system should be in this state
</code></pre></div>"""

[<Test>]
let ``Tags``() =
    """
    Feature: First feature

    @some-tag @one-more-tag
    Scenario: One
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario">
        <h3 class="gherkin-scenario-title">One</h3>
        <div><span class="gherkin-tags">Tags:</span>some-tag, one-more-tag</div>
        <pre class="gherkin-scenario-body">"""

[<Test>]
let ``Comments``() =
    """
    Feature: First feature

    Scenario: One
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state

    # this is a comment
    # over multiple lines
    @some-tag @one-more-tag
    Scenario: Two
    GIVEN some environment
    WHEN some event happens
    THEN the system should be in this state
    """
    |> TestApi.GenerateHtmlDoc
    |> should haveSubstringIgnoringWhitespaces  """
      <div class="gherkin-scenario">
        <h3 class="gherkin-scenario-title">One</h3>
        <pre class="gherkin-scenario-body"><code><span class="gherkin-keyword">Given</span> some environment
<span class="gherkin-keyword">When</span> some event happens
<span class="gherkin-keyword">Then</span> the system should be in this state
</code></pre>
      </div>
      <div class="gherkin-scenario">
        <h3 class="gherkin-scenario-title">Two</h3>
        <div><span class="gherkin-tags">Tags:</span>some-tag, one-more-tag</div>
        <div class="gherkin-description">this is a comment over multiple lines</div>
        <pre class="gherkin-scenario-body"><code><span class="gherkin-keyword">Given</span> some environment
<span class="gherkin-keyword">When</span> some event happens
<span class="gherkin-keyword">Then</span> the system should be in this state
</code></pre>"""

