Feature: Bullet Time

  Scenario: Who is the one?
    Given the following actors:
		* Keanu Reeves
		* Bruce Willis
		* Johnny Depp
	When the following are not available:
		* Johnny Depp
		* Bruce Willis
	Then Keanu Reeves is the obvious choice
