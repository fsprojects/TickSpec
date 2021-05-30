Feature: Placeholders in items feature
Scenario Outline: Placeholders in items test scenario
Given I have a table with placeholders
    | col1             | 
    | <Placeholder1>   |
When I take a doc string with placeholders
    """
    First line with placeholder <Placeholder1>
        Second line
    Third line
    """
Then I can even have a bullet list with placeholders
    * First item with placeholder <Placeholder1>
    * Second item

Examples: 
    | Placeholder1 |
    | Value1       |
    | Value2       |