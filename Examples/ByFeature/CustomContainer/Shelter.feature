Feature: Contrived Dependency Injection Scenario
# to be replaced by something real, if you use it, it's on you!

Scenario: Counting animals
	Given a cattery with 20 spaces
	And a kennel with 10 spaces
	When I bring 5 dogs
	And I bring 6 cats
	Then 19 sheltering slots remain

Scenario: Counting cats
	Given a cattery with 20 spaces
	And a kennel with 0 spaces
	Then 20 sheltering slots remain