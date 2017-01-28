namespace Astrid.Mobile.Shared

open System.Collections.Generic
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
    type [<Table("PlaceOfInterest")>] PlaceOfInterestEntity(placeOfInterest: PlaceOfInterest, location: GeodesicLocation) =
        [<PrimaryKey; AutoIncrement>] member val Id = 0 with get, set
        [<MaxLength(256); Unique>] member val Label = placeOfInterest.Label with get, set
        member val LatitudeDegrees = location.Latitude / 1.0<deg> with get, set
        member val LongitudeDegrees = location.Longitude / 1.0<deg> with get, set
        [<OneToMany(CascadeOperations = CascadeOperation.All)>] member val Address = new List<PlaceOfInterestAddressLineEntity>() with get, set
        new() = new PlaceOfInterestEntity({ Label = String.Empty; Address = [||] }, new GeodesicLocation())
        member this.PlaceOfInterest() =
            {
                Label = this.Label;
                Address = this.Address |> Seq.map (fun lineEntity -> lineEntity.Line) |> Array.ofSeq
            }
        member this.Location = new GeodesicLocation(1.0<deg> * this.LatitudeDegrees, 1.0<deg> * this.LongitudeDegrees)

    and [<Table("PlaceOfInterestAddress")>] PlaceOfInterestAddressLineEntity(line: string) =
        [<PrimaryKey; AutoIncrement>] member val Id = 0 with get, set
        [<MaxLength(256)>] member val Line = line with get, set
        [<ForeignKey(typeof<PlaceOfInterestEntity>)>] member val PlaceOfInterestId = 0 with get, set
        [<ManyToOne>] member val PlaceOfInterest = Unchecked.defaultof<PlaceOfInterestEntity> with get, set
        new() = new PlaceOfInterestAddressLineEntity(String.Empty)

open SqliteEntities
type PlaceOfInterestRepository(platform, dbPath) =
    let conn = new SQLiteAsyncConnection(fun() -> (new SQLiteConnectionWithLock(platform, dbPath)))
    let placesOfInterest (entities:PlaceOfInterestEntity seq) = entities |> Seq.map (fun p -> (p.PlaceOfInterest(), p.Location)) |> Array.ofSeq
    do
        conn.CreateTableAsync<PlaceOfInterestEntity>().Wait()
        conn.CreateTableAsync<PlaceOfInterestAddressLineEntity>().Wait()
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
            let addPlaceOfInterest (c:SQLiteConnection) = c.InsertWithChildren(new PlaceOfInterestEntity(placeOfInterest, location)) |> ignore
            do! conn.RunInTransactionAsync(addPlaceOfInterest) |> Async.AwaitTask
        }
    member __.GetAllPlacesOfInterestAsync() =
        async {
            let! entities = conn.GetAllWithChildrenAsync<PlaceOfInterestEntity>() |> Async.AwaitTask
            return entities |> placesOfInterest
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
