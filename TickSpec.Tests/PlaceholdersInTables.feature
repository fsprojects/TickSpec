Feature: Placeholders in tables feature
Scenario Outline: Placeholders in tables test scenario
Given I have a table with placeholders
    | col1             | 
    | <Placeholder1>   | 
When I take a table with placeholders
    | col1             | 
    | <Placeholder1>   | 
Then I can compare with a table with placeholders
    | col1             | 
    | <Placeholder1>   | 

Examples: 
    | Placeholder1 |
    | Value1       |
    | Value2       |