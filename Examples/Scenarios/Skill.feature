Feature: Skill availability
  As a ninja trainer, 
  I want ninjas to understand the dangers of various opponents 
  so that they can engage them in combat more effectively 

  Background: Allowed skills
    Given the following skills are allowed
      | katana          |
  	  | karate-kick     |
      | roundhouse-kick |

  Scenario: Samurai are dangerous with katanas, no advanced kicks
    When a ninja faces a samurai
    Then he should expect the following attack techniques
      | technique     | danger | 
      | katana        | high   |
      | karate-kick   | low    |

  Scenario: Chuch Norris can do anything and is always dangerous 
    When a ninja faces Chuck Norris
    Then he should expect the following attack techniques 
      | technique       | danger   |
      | katana          | extreme  |
      | karate-kick     | extreme  |    
      | roundhouse-kick | extreme  |