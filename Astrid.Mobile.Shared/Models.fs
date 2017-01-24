namespace Astrid.Mobile.Shared

open GeographicLib

[<StructuralEquality; NoComparison>]
type PlaceOfInterest = 
    {
        Label: string
        Address: string[]
        Location: GeodesicLocation
    }
