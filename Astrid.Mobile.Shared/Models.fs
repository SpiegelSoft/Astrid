namespace Astrid.Mobile.Shared

[<StructuralEquality; NoComparison>]
type PlaceOfInterest = 
    {
        Label: string
        Address: string[]
    }
