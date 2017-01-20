namespace Astrid.Mobile.Shared

open GeographicLib

type PlaceOfInterest = 
    {
        Label: string
        Address: string[]
        Location: GeodesicLocation
    }
