Feature: Shopping

Scenario: Shopping in a store
Given I have 100 dollars in my wallet
When I buy an item for 5 dollars
And I buy an item for 10 dollars
Then My wallet contains 85 dollars