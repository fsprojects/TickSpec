Feature: Web requests

Scenario: Google contains Google
	When I download https://www.google.com/ web page using Async
	Then the downloaded page contains "Google"

Scenario: Bing contains Bing
	When I download https://www.bing.com/ web page using Tasks
	Then the downloaded page contains "Bing"