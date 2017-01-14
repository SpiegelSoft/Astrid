namespace Astrid.Mobile.Shared

open System.Collections.ObjectModel
open System.Reactive.Linq
open System

open ReactiveUI

open XamarinForms.Reactive.FSharp

open Astrid.Localisation

open LocatorDefaults

open GeographicLib

type DashboardViewModel(?host: IScreen, ?platform: IAstridPlatform) as this =
    inherit ReactiveViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let searchResults = new ObservableCollection<GeodesicLocation>()
    let geocodeAddress(vm: DashboardViewModel) =
        let vm = match box vm with | null -> this | _ -> vm
        searchResults.Clear()
        async {
            let! results = platform.Geocoder.GetPositionsForAddressAsync(vm.SearchAddress) |> Async.AwaitTask
            results |> Seq.map (fun r -> new GeodesicLocation(r.Latitude * 1.0<deg>, r.Longitude * 1.0<deg>)) |> Seq.iter searchResults.Add
            match results |> Seq.tryLast with
            | Some position -> vm.Location <- position |> XamarinGeographic.geodesicLocation
            | None -> results |> ignore
        } |> Async.StartAsTask
    let searchForAddress = ReactiveCommand.CreateFromTask geocodeAddress
    let mutable searchAddress = String.Empty
    override this.SubscribeToNotifications() = base.Watch(this, <@ fun vm -> vm.Location @>, ["Location"])
    member __.Title with get() = LocalisedStrings.AppTitle
    member __.SearchForAddress with get() = searchForAddress
    member this.SearchAddress with get() = searchAddress and set(value) = this.RaiseAndSetIfChanged(&searchAddress, value, "SearchAddress") |> ignore
    member val Location = new GeodesicLocation(51.4<deg>, 0.02<deg>) with get, set
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
