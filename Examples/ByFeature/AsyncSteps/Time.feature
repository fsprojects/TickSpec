Feature: Time

Scenario: Time
	Given having current time
	When I sleep for 20ms using Async
	And I sleep for 20ms using Tasks
	Then the current time is at least 40ms higher than it was
