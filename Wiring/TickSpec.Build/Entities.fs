namespace TickSpec.Build

type Scenario = {
    Name : string
    Title : string
    Description : string
    Tags : string list
    Body : string list
    StartsAtLine : int
}

type Location = {
    Filename : string
    /// project local folders
    Folders : string list
}

type Feature = {
    Name : string
    Location : Location
    Background : string list
    Scenarios : Scenario list
}
