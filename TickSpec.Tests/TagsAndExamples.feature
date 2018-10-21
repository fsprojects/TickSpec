@http
Feature: HTTP server

Background:
Given User connects to <server>

@basics @index
Scenario Outline: Tags and Examples
When Client requests <page>
Then Server responds with page <page>

@smoke @all
Examples:
    | server  |
    | smoke   |

Examples:
    | page         |
    | index.html   |
    | default.html |

@all
Shared Examples:
    | server     |
    | testing    |
    | production |
