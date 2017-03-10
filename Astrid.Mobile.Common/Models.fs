namespace Astrid.Mobile.Common

[<StructuralEquality; NoComparison>]
type PlaceOfInterest = 
    {
        PlaceOfInterestId: int
        Label: string
        Address: string[]
    }

[<StructuralEquality; NoComparison>]
type SearchResult =
    {
        SearchTerm: string
        Address: string[]
    }
