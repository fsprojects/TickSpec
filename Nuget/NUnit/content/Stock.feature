Feature: Refunded or replaced items should be returned to stock

Scenario 1: Refunded items should be returned to stock
	Given a customer buys a black jumper
	And I have 3 black jumpers left in stock 
	When he returns the jumper for a refund 
	Then I should have 4 black jumpers in stock
