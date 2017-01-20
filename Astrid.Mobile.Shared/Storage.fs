namespace Astrid.Mobile.Shared

open System.Linq
open System

open XamarinForms.Reactive.FSharp

open SQLite
open System.Threading.Tasks

type [<Table("PlaceOfInterest")>] PlaceOfInterestEntity() =
    [<PrimaryKey; AutoIncrement>] member val Id = 0 with get, set
    [<MaxLength(256); Unique>] member val Label = String.Empty with get, set
    member val LatitudeDegrees = 0.0 with get, set
    member val LongitudeDegrees = 0.0 with get, set

and [<Table("PlaceOfInterestAddress")>] PlaceOfInterestAddressLineEntity() =
    [<PrimaryKey; AutoIncrement>] member val Id = 0 with get, set
    [<MaxLength(256)>] member val Line = String.Empty with get, set
    [<Indexed>] member val PlaceOfInterestId = 0 with get, set

type PlaceOfInterestRepository(dbPath) =
    let conn = new SQLiteAsyncConnection(dbPath)
    do
        conn.CreateTableAsync<PlaceOfInterestEntity>().Wait()
        conn.CreateTableAsync<PlaceOfInterestAddressLineEntity>().Wait()
    member __.AddPlaceOfInterestAsync(placeOfInterest:PlaceOfInterest) =
        async {
            let position = XamarinGeographic.position placeOfInterest.Location
            let addPlaceOfInterest (c:SQLiteConnection) =
                let placeOfInterestId = c.Insert(new PlaceOfInterestEntity(Label = placeOfInterest.Label, LatitudeDegrees = position.Latitude, LongitudeDegrees = position.Longitude))
                for line in placeOfInterest.Address do c.Insert(new PlaceOfInterestAddressLineEntity(Line = line, PlaceOfInterestId = placeOfInterestId)) |> ignore
            do! conn.RunInTransactionAsync(addPlaceOfInterest) |> Async.AwaitTask
        }
    member __.GetAllPlacesOfInterest() =
        async {
            let! allPlacesOfInterest = conn.QueryAsync<PlaceOfInterestEntity>("SELECT Id, Label, LatitudeDegrees, LongitudeDegrees FROM PlaceOfInterest") |> Async.AwaitTask
            let x = conn.Table<PlaceOfInterestAddressLineEntity>(). allPlacesOfInterest.Select(fun p -> p.Label).to
            //for place
        }
    interface IDisposable with member __.Dispose() = conn.Dispose()
