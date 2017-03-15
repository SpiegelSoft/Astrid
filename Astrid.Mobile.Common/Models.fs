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
    static member DefaultValue = 
        { 
            PlaceOfInterestId = 0; 
            Label = String.Empty; 
            Description = None; 
            Image = HeadlineImage.PlaceholderImage; 
            Address = [||] 
        }

[<StructuralEquality; NoComparison>]
type TimelineEvent =
    {
        TimelineEventId: int
        Date: DateTime
        Title: string option
        EventDescription: string
        Image: HeadlineImage
    }
    static member DefaultValue = 
        { 
            TimelineEventId = 0
            Date = DateTime.MinValue 
            Title = None
            EventDescription = String.Empty
            Image = HeadlineImage.PlaceholderImage
        }

[<StructuralEquality; NoComparison>]
type SearchResult =
    {
        SearchTerm: string
        Address: string[]
    }
