Feature: Scrabble score

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