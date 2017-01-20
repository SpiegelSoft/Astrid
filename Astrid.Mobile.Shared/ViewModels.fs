namespace Astrid.Mobile.Shared

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open FSharp.Collections.ParallelSeq

open ReactiveUI

open XamarinForms.Reactive.FSharp

open Astrid.Localisation

open LocatorDefaults

open GeographicLib

type SearchResult =
    {
        SearchedForAddress: string
        FoundAddress: string
        Location: GeodesicLocation
    }

type DashboardViewModel(?host: IScreen, ?platform: IAstridPlatform) as this =
    inherit ReactiveViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let geocoder = platform.Geocoder
    let searchResults = new ReactiveList<SearchResult>()
    let commandSubscriptions = new CompositeDisposable()
    let geocodeAddress(vm: DashboardViewModel) =
        let vm = match box vm with | null -> this | _ -> vm
        async {
            let! results = geocoder.GetPositionsForAddressAsync(vm.SearchAddress) |> Async.AwaitTask
            return results |> Seq.map (fun r -> new GeodesicLocation(r.Latitude * 1.0<deg>, r.Longitude * 1.0<deg>))
        } |> Async.StartAsTask
    let showResults res = 
        searchResults.Clear()
        res |> ignore
    let searchForAddress = ReactiveCommand.CreateFromTask geocodeAddress
    let mutable searchAddress = String.Empty
    let mutable location = new GeodesicLocation(51.4<deg>, 0.02<deg>)
    override __.SubscribeToCommands() = searchForAddress.ObserveOn(RxApp.MainThreadScheduler).Subscribe(showResults) |> commandSubscriptions.Add
    override __.UnsubscribeFromCommands() = commandSubscriptions.Clear()
    member __.Title with get() = LocalisedStrings.AppTitle
    member __.SearchForAddress with get() = searchForAddress
    member this.SearchAddress with get() = searchAddress and set(value) = this.RaiseAndSetIfChanged(&searchAddress, value, "SearchAddress") |> ignore
    member this.Location with get() = location and set(value) = this.RaiseAndSetIfChanged(&location, value, "Location") |> ignore
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
