namespace Astrid.Mobile.Shared

open System.Collections.ObjectModel
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
        } |> Async.StartAsTask
    let searchForAddress = ReactiveCommand.CreateFromTask geocodeAddress
    let mutable location = new GeodesicLocation()
    let mutable searchAddress = String.Empty
    member __.Title with get() = LocalisedStrings.AppTitle
    member __.SearchForAddress with get() = searchForAddress
    member this.SearchAddress with get() = searchAddress and set(value) = this.RaiseAndSetIfChanged(&searchAddress, value, "SearchAddress") |> ignore
    member this.LatitudeDegrees 
        with get() = location.Latitude / 1.0<deg> 
        and set(value) = location <- new GeodesicLocation(value * 1.0<deg>, location.Longitude); this.RaisePropertyChanged("LatitudeDegrees")
    member this.LongitudeDegrees 
        with get() = location.Longitude / 1.0<deg> 
        and set(value) = location <- new GeodesicLocation(location.Latitude, value * 1.0<deg>); this.RaisePropertyChanged("LongitudeDegrees")
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
