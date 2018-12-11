Feature: Shopping

This sample demonstrates the usage of Tables, Lists and Doc Strings with Functional Injection.

Background: The product catalog is updated each year
  Given no catalogs prior to 2018
  And the 2018 product catalog:
    |Id |Description |Price|
    |123|Black Jumper|5    |
    |456|Blue Jeans  |10   |
  And the 2019 product catalog:
    |Id |Description |Price|
    |789|Red Socks   |3    |
    |876|Sunglasses  |55   |

Scenario: Making a purchase
  When I make a purchase on 2018-12-22:
    * Blue Jeans
    * Black Jumper
  Then the receipt dated 2018-12-22 includes:
      """
      Blue Jeans: $10.00
      Black Jumper: $5.00
      """