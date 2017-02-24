namespace Astrid.Mobile.Common

[<StructuralEquality; NoComparison>]
type PlaceOfInterest = 
    {
        Label: string
        Address: string[]
    }
