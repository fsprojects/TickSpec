Feature: Shopping

This sample demonstrates the usage of Tables, Lists and Doc Strings with Functional Injection.

Scenario: Making a purchase
  Given the 2018 product catalog:
    |Id |Description |Price|
    |123|Black Jumper|5    |
    |456|Blue Jeans  |10   |
  When I make an order:
    * Blue Jeans
    * Black Jumper
  Then the receipt dated 2018-12-22 includes:
      """
      Thankyou for your purchase.
       - Black Jumper: $5.00
       - Blue Jeans: $10.00
      Total: $15.00
      """