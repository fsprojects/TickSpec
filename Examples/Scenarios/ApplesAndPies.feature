Feature: Apples And Pies
  Apples and Apple Pies are different things. We know this. You can eat whichever you choose.

  Scenario: I ♥ Apple
    When I eat an apple
    Then 1 apples have been eaten
	And 0 apple pies have been eaten

  Scenario: PIES!
    When I eat an apple pie
    Then 0 apples have been eaten
	And 1 apple pies have been eaten

  Scenario: Greedy!
    When I eat an apple
	And I eat an apple pie
    Then 1 apples have been eaten
	And 1 apple pies have been eaten
