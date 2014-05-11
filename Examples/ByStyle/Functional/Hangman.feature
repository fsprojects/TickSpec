Feature: Play Hangman

Background:
 Given the word 'HANGMAN'
 And no guesses

Scenario: Letter 'A' occurs
 When the letter 'A' is guessed
 Then the display word is '_A___A_'

Scenario: Letter 'B' does not occur
 When the letter 'B' is guessed
 Then the tally is 1

Scenario: Letters 'BC' do not occur
 When the letter 'B' is guessed
 And  the letter 'C' is guessed
 Then the tally is 2

Scenario: Letters 'ABH' guessed
 When the letter 'A' is guessed
 And  the letter 'B' is guessed
 And  the letter 'H' is guessed
 Then the tally is 1
 Then the display word is 'HA___A_'