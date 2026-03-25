Feature: Escaped characters in table cells

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

    Scenario Outline: Values with escaped backslashes are parsed correctly
        Given a value <value>
        Then the value is <expected>

        Examples:
            | value              | expected         |
            | hello\\world       | hello\\world     |

    Scenario Outline: Values with escaped newlines are parsed correctly
        Given a value <value>
        Then the value is <expected>

        Examples:
            | value              | expected         |
            | line1\nline2       | line1\nline2     |

    Scenario: Step table with escaped pipes
        Given a table with escaped pipes
            | col1            | col2   |
            | hello\|world    | normal |
            | a\|b\|c         | x      |
        Then the table cell 0,0 is hello|world
        And the table cell 0,1 is normal
        And the table cell 1,0 is a|b|c

    Scenario: Step table with escaped backslashes
        Given a table with escaped backslashes
            | col1            | col2   |
            | hello\\world    | normal |
        Then the table cell 0,0 is hello\world
        And the table cell 0,1 is normal

    Scenario: Escaped backslash before pipe acts as cell delimiter
        Given a table with escaped backslash before pipe
            | col1    | col2   | col3 |
            | before\\| after  | end  |
        Then the table cell 0,0 is before\
        And the table cell 0,1 is after
        And the table cell 0,2 is end

    Scenario: Triple backslash-pipe is escaped backslash then escaped pipe
        Given a table with triple backslash-pipe
            | col1     | col2   |
            | a\\\|b   | normal |
        Then the table cell 0,0 is a\|b
        And the table cell 0,1 is normal

    Scenario: Escaped backslash before n is literal backslash-n not newline
        Given a table with escaped backslash before n
            | col1     | col2   |
            | test\\n  | normal |
        Then the table cell 0,0 is test\n
        And the table cell 0,1 is normal
