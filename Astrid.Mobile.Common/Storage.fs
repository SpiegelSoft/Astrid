namespace Astrid.Mobile.Common

open System.Collections.Generic
open System.Threading.Tasks
open System

open SQLiteNetExtensionsAsync.Extensions
open SQLiteNetExtensions.Extensions
open SQLiteNetExtensions.Attributes
open SQLite.Net.Attributes
open SQLite.Net.Async
open SQLite.Net

open GeographicLib

open Xamarin.Forms.Maps

open XamarinForms.Reactive.FSharp.ExpressionConversion
open XamarinForms.Reactive.FSharp

module SqliteEntities =
    type IHeadlineImage =
        abstract ImageFile: string
        abstract ImageUrl: string

    let private toImage (headlineImage: IHeadlineImage) =
        match (headlineImage.ImageFile, headlineImage.ImageUrl) with | (f, _) when not <| String.IsNullOrWhiteSpace(f) -> ImageFile f | (_, u) when not <| String.IsNullOrWhiteSpace(u) -> ImageUrl (new Uri(u)) | _ -> PlaceholderImage
    let private toImageFileRecord image = match image with | ImageFile file -> file | _ -> String.Empty
    let private toImageUrlRecord image = match image with | ImageUrl url -> url.ToString() | _ -> String.Empty

    type [<Table("PlaceOfInterest")>] PlaceOfInterestEntity(placeOfInterest: PlaceOfInterest, location: GeodesicLocation) =
        [<PrimaryKey; AutoIncrement>] member val Id = 0 with get, set
        [<MaxLength(256); Unique>] member val Label = placeOfInterest.Label with get, set
        [<MaxLength(1024)>] member val Description = (match placeOfInterest.Description with | Some description -> description | None -> Unchecked.defaultof<string>) with get, set
        [<MaxLength(512)>] member val ImageFile = placeOfInterest.Image |> toImageFileRecord with get, set
        [<MaxLength(512)>] member val ImageUrl = placeOfInterest.Image |> toImageUrlRecord with get, set
        member val LatitudeDegrees = location.Latitude / 1.0<deg> with get, set
        member val LongitudeDegrees = location.Longitude / 1.0<deg> with get, set
        [<OneToMany(CascadeOperations = CascadeOperation.All)>] member val Address = new List<PlaceOfInterestAddressLineEntity>() with get, set
        new() = new PlaceOfInterestEntity(PlaceOfInterest.DefaultValue, new GeodesicLocation())
        member this.PlaceOfInterest() =
            {
                PlaceOfInterestId = this.Id
                Label = this.Label
                Description = match this.Description with | es when String.IsNullOrEmpty(es) -> None | _ -> Some this.Description
                Image = this |> toImage
                Address = this.Address |> Seq.map (fun lineEntity -> lineEntity.Line) |> Array.ofSeq
            }
        member this.Location = new GeodesicLocation(1.0<deg> * this.LatitudeDegrees, 1.0<deg> * this.LongitudeDegrees)
        interface IHeadlineImage with member this.ImageFile = this.ImageFile member this.ImageUrl = this.ImageUrl

    and [<Table("PlaceOfInterestAddress")>] PlaceOfInterestAddressLineEntity(line: string) =
        [<PrimaryKey; AutoIncrement>] member val Id = 0 with get, set
        [<MaxLength(256)>] member val Line = line with get, set
        [<ForeignKey(typeof<PlaceOfInterestEntity>)>] member val PlaceOfInterestId = 0 with get, set
        [<ManyToOne>] member val PlaceOfInterest = Unchecked.defaultof<PlaceOfInterestEntity> with get, set
        new() = new PlaceOfInterestAddressLineEntity(String.Empty)

    type [<Table("TimelineEvent")>] TimelineEventEntity(placeOfInterest: PlaceOfInterest, timelineEvent: TimelineEvent) =
        [<PrimaryKey; AutoIncrement>] member val Id = timelineEvent.TimelineEventId with get, set
        member val PlaceOfInterestId = placeOfInterest.PlaceOfInterestId with get, set
        member val Date = timelineEvent.Date with get, set
        [<MaxLength(256)>] member val Title = (match timelineEvent.Title with | Some te -> te | None -> String.Empty) with get, set
        member val EventDescription = timelineEvent.EventDescription with get, set
        [<MaxLength(512)>] member val ImageFile = timelineEvent.Image |> toImageFileRecord with get, set
        [<MaxLength(512)>] member val ImageUrl = timelineEvent.Image |> toImageUrlRecord with get, set
        interface IHeadlineImage with member this.ImageFile = this.ImageFile member this.ImageUrl = this.ImageUrl
        new() = new TimelineEventEntity(PlaceOfInterest.DefaultValue, TimelineEvent.DefaultValue)
        member this.TimelineEvent() =
            {
                TimelineEventId = this.Id
                Date = this.Date
                Title = match String.IsNullOrWhiteSpace(this.Title) with | false -> None | true -> Some this.Title
                EventDescription =  this.EventDescription
                Image = this |> toImage
            }

open SqliteEntities
type PlaceOfInterestRepository(platform, dbPath) =
    let conn = new SQLiteAsyncConnection(fun() -> (new SQLiteConnectionWithLock(platform, dbPath)))
    do 
        conn.CreateTableAsync<TimelineEventEntity>().Wait()
        conn.CreateTableAsync<PlaceOfInterestAddressLineEntity>().Wait()
        conn.CreateTableAsync<PlaceOfInterestEntity>().Wait()
    let placesOfInterest (entities:PlaceOfInterestEntity seq) = entities |> Seq.map (fun p -> (p.PlaceOfInterest(), p.Location)) |> Array.ofSeq
    let placesOfInterestInBoundingBox (northWest: GeodesicLocation) (southEast: GeodesicLocation) =
        let southLatitudeDegrees, northLatitudeDegrees = southEast.Latitude / 1.0<deg>, northWest.Latitude / 1.0<deg>
        let westLongitudeDegrees, eastLongitudeDegrees = northWest.Longitude / 1.0<deg>, southEast.Longitude / 1.0<deg>
        let regionCrossesInternationalDateLine = southEast.Longitude < northWest.Longitude
        let liesInRegion = 
            match regionCrossesInternationalDateLine with
            | false -> toLinq <@ fun (p:PlaceOfInterestEntity) -> p.LatitudeDegrees >= southLatitudeDegrees && p.LatitudeDegrees <= northLatitudeDegrees && p.LongitudeDegrees >= westLongitudeDegrees && p.LongitudeDegrees <= eastLongitudeDegrees @>
            | true -> toLinq <@ fun (p:PlaceOfInterestEntity) -> p.LatitudeDegrees >= southLatitudeDegrees && p.LatitudeDegrees <= northLatitudeDegrees && not (p.LongitudeDegrees < westLongitudeDegrees && p.LongitudeDegrees > eastLongitudeDegrees) @>
        async {
            let! entities = conn.GetAllWithChildrenAsync<PlaceOfInterestEntity>(liesInRegion) |> Async.AwaitTask
            return entities |> placesOfInterest
        }
    member __.AddPlaceOfInterestAsync(placeOfInterest: PlaceOfInterest, location: GeodesicLocation) =
        async {
            let addPlaceOfInterest (c:SQLiteConnection) = c.InsertWithChildren(new PlaceOfInterestEntity(placeOfInterest, location, Address = new List<PlaceOfInterestAddressLineEntity>(placeOfInterest.Address |> Seq.map (fun lineEntity -> new PlaceOfInterestAddressLineEntity(lineEntity)) |> Array.ofSeq))) |> ignore
            do! conn.RunInTransactionAsync(addPlaceOfInterest) |> Async.AwaitTask
        }
    member __.GetAllPlacesOfInterestAsync() =
        async {
            let! entities = conn.GetAllWithChildrenAsync<PlaceOfInterestEntity>() |> Async.AwaitTask
            return entities |> placesOfInterest
        }
    member __.GetTimelineEventsAsync(placeOfInterest: PlaceOfInterest) =
        async {
            let placeOfInterestId = placeOfInterest.PlaceOfInterestId
            let! timelineEventEntities = conn.GetAllWithChildrenAsync<TimelineEventEntity>(toLinq <@ fun (e: TimelineEventEntity) -> e.PlaceOfInterestId = placeOfInterestId @>) |> Async.AwaitTask
            return timelineEventEntities |> Seq.map (fun e -> e.TimelineEvent()) |> Array.ofSeq
        }
    member __.SearchForPlacesOfInterestAsync(searchTerm) =
        async {
            let! entities = conn.GetAllWithChildrenAsync(toLinq <@ fun (p:PlaceOfInterestEntity) -> p.Label.Contains(searchTerm) || p.Description.Contains(searchTerm) @>) |> Async.AwaitTask
            return entities |> placesOfInterest
        }
    member __.DeletePlaceOfInterestAsync(placeOfInterestId: int) =
        async {
            let deletePlaceOfInterest (c:SQLiteConnection) =
                let placeOfInterest = c.Get<PlaceOfInterestEntity>(placeOfInterestId)
                match box placeOfInterest with
                | null -> placeOfInterest |> ignore
                | _ ->
                    c.DeleteAll(placeOfInterest.Address)
                    c.Delete<PlaceOfInterestEntity>(placeOfInterestId) |> ignore
            do! conn.RunInTransactionAsync(deletePlaceOfInterest) |> Async.AwaitTask
        }
    member __.GetPlacesOfInterestAsync(centre: GeodesicLocation, radius: float<m>) =
        let geodesic = Geodesic.WGS84
        let westSide, eastSide = geodesic.Location centre -90.0<deg> radius, geodesic.Location centre 90.0<deg> radius
        let northWest, southEast = geodesic.Location westSide 0.0<deg> radius, geodesic.Location eastSide 180.0<deg> radius
        async {
            let! placesOfInterest = placesOfInterestInBoundingBox northWest southEast
            return placesOfInterest |> Array.filter (fun (p, l) -> geodesic.Distance l centre <= radius)
        }

type IAstridPlatform = 
    inherit IPlatform
    abstract member Geocoder: Geocoder
    abstract member PlacesOfInterest: PlaceOfInterestRepository
