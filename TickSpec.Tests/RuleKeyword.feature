Feature: Rule keyword support

  Background:
    Given feature-level setup

  Scenario: Direct scenario without rule
    When running outside rule
    Then it works

  Rule: First business rule
    Background:
      Given rule-level setup for first rule

    Scenario: Scenario in first rule
      When performing first rule action
      Then first rule result is observed

    Scenario: Another scenario in first rule
      When doing something else in first rule
      Then another result

  Rule: Second business rule
    Scenario: Scenario in second rule
      When performing second rule action
      Then second rule result is observed
