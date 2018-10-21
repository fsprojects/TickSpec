Feature: Web login testing

    Scenario Outline: Successful login
    Given I use browser <browser>
    When I try to login using username <username> and password <password>
    Then I am logged in

    Examples:
    | browser       |
    | Firefox       |
    | Edge          |
    | Chrome        |

    @Smoke
    Examples:
    | browser       |
    | Firefox       |

    Scenario Outline: Show page
    Given I use browser <browser>
    When I go to the main page
    Then The main page is displayed

Shared Examples:
| username      | password      |
| test          | psw           |
| test2         | psw2          |