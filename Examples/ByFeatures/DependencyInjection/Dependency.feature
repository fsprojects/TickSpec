Feature: Dependency

    Scenario: Store and retrieve the value
    Given I use the <Implementation> implementation
    When I store "<Text>"
    Then I retrieve "<Output>"

    Examples:
    | Implementation    | Text      | Output        |
    | first             | test      | First: test   |
    | second            | test      | Second: test  |

    Scenario: Store and retrieve the value using the second implementation
    When I store "test" using the second implementation
    Then I retrieve "Second: test" using the second implementation
