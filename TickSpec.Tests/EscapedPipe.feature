Feature: Escaped pipe in table cells

    Scenario Outline: Values with escaped pipes are parsed correctly
        Given a value <value>
        Then the value is <expected>

        Examples:
            | value              | expected         |
            | no pipe            | no pipe          |
            | before\|after      | before\|after    |
            | \|leading          | \|leading        |
            | trailing\|         | trailing\|       |
            | a\|b\|c            | a\|b\|c          |

    Scenario: Step table with escaped pipes
        Given a table with escaped pipes
            | col1            | col2   |
            | hello\|world    | normal |
            | a\|b\|c         | x      |
        Then the table cell 0,0 is hello|world
        And the table cell 0,1 is normal
        And the table cell 1,0 is a|b|c
