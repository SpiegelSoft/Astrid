namespace Astrid.Mobile.Common

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open ReactiveUI

open XamarinForms.Reactive.FSharp

open Astrid.Localisation

open LocatorDefaults

open GeographicLib

[<StructuralEquality; NoComparison>]
type LocationDetails =
    | SearchResult of string
    | PlaceOfInterest of PlaceOfInterest

type MarkerViewModel(location, details: LocationDetails) =
    inherit ReactiveObject()
    member val Location = location
    member val Details = details

type DashboardViewModel(?host: IScreen, ?platform: IAstridPlatform) as this =
    inherit PageViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let geocoder = platform.Geocoder
    let markers = new ReactiveList<MarkerViewModel>()
    let commandSubscriptions = new CompositeDisposable()
    let geocodeAddress(vm: DashboardViewModel) =
        let searchMarkers = markers |> Seq.choose (fun m -> match m.Details with | SearchResult _ -> Some m | _ -> None) |> Array.ofSeq
        for searchMarker in searchMarkers do markers.Remove(searchMarker) |> ignore
        let vm = match box vm with | null -> this | _ -> vm
        async {
            let searchAddress = vm.SearchAddress
            let! positions = geocoder.GetPositionsForAddressAsync(searchAddress) |> Async.AwaitTask
            let searchResults = positions |> Seq.map (fun r -> new MarkerViewModel(XamarinGeographic.geodesicLocation r, SearchResult searchAddress))
            searchResults |> Seq.iter markers.Add
            return searchResults
        } |> Async.StartAsTask
    let showResults (searchResults: MarkerViewModel seq) = 
        match searchResults |> Seq.tryHead with
        | Some r -> this.Location <- r.Location
        | None -> this.Message <- { Title = LocalisedStrings.NoResultsFound; Message = String.Format(LocalisedStrings.NoResultsFoundForAddress, this.SearchAddress); Accept = LocalisedStrings.OK }
    let searchForAddressCommand = ReactiveCommand.CreateFromTask geocodeAddress
    let mutable searchAddress = String.Empty
    let mutable location = new GeodesicLocation(51.4<deg>, -0.02<deg>)
    override __.SubscribeToCommands() = searchForAddressCommand.ObserveOn(RxApp.MainThreadScheduler).Subscribe(showResults) |> commandSubscriptions.Add
    override __.UnsubscribeFromCommands() = commandSubscriptions.Clear()
    member __.Title with get() = LocalisedStrings.AppTitle
    member __.SearchForAddressCommand with get() = searchForAddressCommand
    member this.SearchAddress with get() = searchAddress and set(value) = this.RaiseAndSetIfChanged(&searchAddress, value, "SearchAddress") |> ignore
    member this.Location with get() = location and set(value) = this.RaiseAndSetIfChanged(&location, value, "Location") |> ignore
    member __.Markers with get() = markers
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
