Feature: Valet parking
 The parking lot calculator can can calculate costs for Valet Parking.

Scenario Outline: Calculate Valet Parking cost
  When I park my car in the Valet Parking Lot for <parking duration>
  Then I will have to pay <parking costs>

Examples:
  | parking duration    | parking costs |
  | 30 minutes          | $ 2.00        |
  | 1 hour              | $ 2.00        |
  | 1 hour 30 minutes   | $ 3.00        |
  | 2 hours             | $ 4.00        |
  | 3 hours 30 minutes  | $ 7.00        |
  | 12 hours 30 minutes | $ 24.00       |
  | 1 day 30 minutes    | $ 25.00       |
  | 1 day 1 hour        | $ 26.00       |

