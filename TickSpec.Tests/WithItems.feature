Feature: Items test feature
Scenario: Items test scenario
Given I have a table
    | col1  | col2  |
    | v11   | v21   |
    | v12   | v22   |
When I take a doc string
    """
    First line
        Second line
    Third line
    """
Then I can take a bullet list
    * First item
    * Second item
And Even the next step is clear
