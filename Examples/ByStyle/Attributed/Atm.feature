Feature: Customer withdraws cash
   As a customer,
   I want to withdraw cash from an ATM,
   so that I don’t have to wait in line at the bank.

Scenario: Account is in credit
   Given the account is in credit
	And the card is valid
	And the dispenser contains cash
   When the customer requests cash
   Then ensure the account is debited
	And ensure cash is dispensed
	And ensure the card is returned

Scenario: Account is overdrawn past the overdraft limit
   Given the account is overdrawn
    And the card is valid
   When the customer requests cash
   Then ensure a rejection message is displayed
    And ensure cash is not dispensed
    And ensure the card is returned