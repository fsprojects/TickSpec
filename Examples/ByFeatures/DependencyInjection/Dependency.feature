Feature: Feeding dog

    Scenario Outline: Feeding dog with enough food
    Given I have a <Size> dog
    When I fill the dog bowl with <Amount>g of food
    And The dog eats the food from bowl
    Then The bowl contains <Residue>g of food

    Examples:
    | Size   | Amount | Residue |
    | small  | 200    | 100     |
    | medium | 250    | 50      |
    | large  | 400    | 0       |