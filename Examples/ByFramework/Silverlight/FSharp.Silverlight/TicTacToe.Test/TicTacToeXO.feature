Feature: Winning positions

Scenario: Winning positions
	Given a board layout:
		| 1   | 2   | 3	  |
		| <O> | <O> | <X> |
		| <O> |     |     |
		| <X> |     | <X> |
	When a player marks <X> at <row> <col>
	And viewed with a <rotation> degree rotation
	Then <X> wins
	
Examples:
	| row    | col	  | 
	| middle | right  |
	| middle | middle |
	| bottom | middle |

Examples:
	| X | O |
	| X | O |
	| O | X |

Shared Examples:
	| rotation |
	| 0        |
	| 90       |
	| 180      |
	| 270      |

