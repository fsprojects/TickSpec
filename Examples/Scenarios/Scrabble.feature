Feature: Scrabble score

Scenario: Quant
 Given an empty scrabble board
 When player 1 plays "QUANT" at 8D 
 And Q is on a DLS
 And T is on the center star
 Then he scores 48

