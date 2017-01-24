namespace Astrid.Mobile.Shared

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open ReactiveUI

open XamarinForms.Reactive.FSharp

open Astrid.Localisation

open LocatorDefaults

open GeographicLib

[<StructuralEquality; NoComparison>]
type SearchResult =
    {
        SearchedForAddress: string
        Location: GeodesicLocation
    }

[<StructuralEquality; NoComparison>]
type MapMarker =
    | SearchResult of SearchResult
    | PlaceOfInterest of PlaceOfInterest

type DashboardViewModel(?host: IScreen, ?platform: IAstridPlatform) as this =
    inherit ReactiveViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let geocoder = platform.Geocoder
    let markers = new ReactiveList<MapMarker>()
    let commandSubscriptions = new CompositeDisposable()
    let geocodeAddress(vm: DashboardViewModel) =
        let vm = match box vm with | null -> this | _ -> vm
        async {
            let searchAddress = vm.SearchAddress
            let! results = geocoder.GetPositionsForAddressAsync(searchAddress) |> Async.AwaitTask
            return results |> Seq.map (fun r -> { SearchedForAddress = searchAddress; Location = new GeodesicLocation(r.Latitude * 1.0<deg>, r.Longitude * 1.0<deg>) })
        } |> Async.StartAsTask
    let showResults searchResults = 
        searchResults |> Seq.map SearchResult |> Seq.iter markers.Add
        match searchResults |> Seq.tryHead with
        | Some r -> this.Location <- r.Location
        | None -> searchResults |> ignore
        this.RaisePropertyChanged("Markers")
    let searchForAddressCommand = ReactiveCommand.CreateFromTask geocodeAddress
    let mutable searchAddress = String.Empty
    let mutable location = new GeodesicLocation(51.4<deg>, -0.02<deg>)
    override __.SubscribeToCommands() = 
        searchForAddressCommand.ObserveOn(RxApp.MainThreadScheduler).Subscribe(showResults) |> commandSubscriptions.Add
    override __.UnsubscribeFromCommands() = commandSubscriptions.Clear()
    member __.Title with get() = LocalisedStrings.AppTitle
    member __.SearchForAddress with get() = searchForAddressCommand
    member this.SearchAddress with get() = searchAddress and set(value) = this.RaiseAndSetIfChanged(&searchAddress, value, "SearchAddress") |> ignore
    member this.Location with get() = location and set(value) = this.RaiseAndSetIfChanged(&location, value, "Location") |> ignore
    member __.Markers with get() = markers
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
