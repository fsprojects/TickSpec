Feature: Pacman score

Scenario Outline: Pacman eats ghosts
  Given the ghosts are vulnerable
  When pacman eats <ghost> in succession
  Then he scores <points>

Examples:
 | ghost | points |
 | 1     | 200    |
 | 2     | 400    |
 | 3     | 800    |
 | 4     | 1600   |

Scenario Outline: Pacman eats fruit
  When pacman easts a <fruit>
  Then he scores <points>

Examples:
  | fruit      | points |
  | cherry     | 100    |
  | strawberry | 300    |
  | orange     | 500    |
  | apple      | 700    |
  | melon      | 1000   |
