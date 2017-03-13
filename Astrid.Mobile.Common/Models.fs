namespace Astrid.Mobile.Common

open System

type HeadlineImage =
    | PlaceholderImage
    | ImageFile of string
    | ImageUrl of Uri

[<StructuralEquality; NoComparison>]
type PlaceOfInterest = 
    {
        PlaceOfInterestId: int
        Label: string
        Description: string option
        Image: HeadlineImage
        Address: string[]
    }

[<StructuralEquality; NoComparison>]
type SearchResult =
    {
        SearchTerm: string
        Address: string[]
    }
