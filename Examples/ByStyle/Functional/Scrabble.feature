Feature: Scrabble score

Scenario: POW
 Given an empty scrabble board
 When player 1 plays "POW" at 8E
 Then he scores 8

Scenario: 2-Letter Words
 Given an empty scrabble board
 When player 1 plays "<word>" at 8E
 Then he scores <score>

Examples:
 | word | score |
 |  AT  |   2   |
 |  DO  |   3   |
 |  BE  |   4   |

Scenario: QUANT
 Given an empty scrabble board
 When player 1 plays "QUANT" at 8D 
 And Q is on a DLS
 And T is on the center star
 Then he scores 48

Scenario: ALIQUANT
 When player 2 prefixes "QUANT" with "ALI" at 8A
 And A is on a TWS
 Then he scores 51

Scenario: OIDIOID
 When player 2 plays "OIDIOID" at 9G
 And O is on a DLS
 And D is on a DLS
 And D is on a DLS
 And forms NO
 And O is on a DLS
 And forms TI
 Then he scores 69