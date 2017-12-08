Feature: Computer heating

Scenario: Computer and temperature
Given room with temperature 10 degrees
When computer is started in the room
And computer runs for 10 minutes
Then computer is on login screen
And room temperature is 11 degrees