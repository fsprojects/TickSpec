Feature: HTTP server

Background:
    Given User connects to <server>

    @GET
    Scenario Outline: GET, <server>
        When Client requests <page>
        Then Server responds with page <page>

    @Smoke
    Examples:
        | server  | page       |
        | testing | index.html |

    @Regression
    Examples:
        | server     | page        |
        | testing    | index.html  |
        | production | index.html  |

    @POST
    Scenario Outline: POST, <server>
        When Client sends <data> to <page>
        Then Server responds with code <errorcode>

    @Smoke
    Examples:
        | server     | data  | page      | errorcode |
        | testing    | 12345 | form.html | 200       |
        | testing    | -1    | form.html | 400       |

    @Regression
    Examples:
        | server     | data  | page      | errorcode |
        | testing    | 12345 | form.html | 200       |
        | testing    | -1    | form.html | 400       |
        | production | 12345 | form.html | 200       |
        | production | -1    | form.html | 400       |