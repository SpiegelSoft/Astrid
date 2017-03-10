namespace Astrid.Mobile.Common

open System.Reactive.Disposables
open System.Threading.Tasks
open System.Reactive.Linq
open System

open ReactiveUI

open XamarinForms.Reactive.FSharp

open Astrid.Localisation

open LocatorDefaults

open GeographicLib

[<StructuralEquality; NoComparison>]
type LocationDetails =
    | SearchResult of SearchResult
    | PlaceOfInterest of PlaceOfInterest

type TimelineViewModel(placeOfInterest: PlaceOfInterest, ?host: IScreen) =
    inherit PageViewModel()
    let host = LocateIfNone host
    override __.SubscribeToCommands() = host |> ignore
    override __.UnsubscribeFromCommands() = host |> ignore
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Timeline"

type MarkerViewModel(location, details: LocationDetails, ?host: IScreen) =
    inherit ReactiveObject()
    let host = LocateIfNone host
    let placeOfInterest =
        match details with
        | SearchResult result -> { PlaceOfInterestId = 0; Label = result.SearchTerm; Address = result.Address }
        | PlaceOfInterest poi -> poi
    let editTimeline(vm: MarkerViewModel) = 
        async {
                host.Router.Navigate.Execute(new TimelineViewModel(placeOfInterest, host)) |> ignore
                return true
            } |> Async.StartAsTask
    member __.EditTimelineCommand with get() = ReactiveCommand.CreateFromTask editTimeline
    member val Location = location
    member val Details = details
    member this.Text =
        match this.Details with
        | SearchResult result -> result.SearchTerm
        | PlaceOfInterest poi -> poi.Label
    member this.HeadlineTextExpression() =
        match details with
        | SearchResult result -> 
            let searchTerm = result.SearchTerm
            <@ fun (vm: MarkerViewModel) -> searchTerm @>
        | PlaceOfInterest poi -> 
            let label = poi.Label
            <@ fun (vm: MarkerViewModel) -> label @>

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
            let searchTerm = vm.SearchTerm
            let! positions = geocoder.GetPositionsForAddressAsync(searchTerm) |> Async.AwaitTask
            let! nearby = positions |> Seq.map (fun pos -> geocoder.GetAddressesForPositionAsync(pos).ContinueWith(fun (t: Task<string seq>) -> (pos, t.Result)) |> Async.AwaitTask) |> Async.Parallel
            let searchResults = nearby |> Seq.map (fun (pos, address) -> new MarkerViewModel(XamarinGeographic.geodesicLocation pos, SearchResult { SearchTerm = searchTerm; Address = address |> Array.ofSeq }))
            searchResults |> Seq.iter markers.Add
            return searchResults
        } |> Async.StartAsTask
    let showResults (searchResults: MarkerViewModel seq) = 
        match searchResults |> Seq.tryHead with
        | Some r -> this.Location <- r.Location
        | None -> this.Message <- { Title = LocalisedStrings.NoResultsFound; Message = String.Format(LocalisedStrings.NoResultsFoundForAddress, this.SearchTerm); Accept = LocalisedStrings.OK }
    let searchForAddressCommand = ReactiveCommand.CreateFromTask geocodeAddress
    let mutable searchTerm = String.Empty
    let mutable location = new GeodesicLocation(51.4<deg>, -0.02<deg>)
    override __.SubscribeToCommands() = searchForAddressCommand.ObserveOn(RxApp.MainThreadScheduler).Subscribe(showResults) |> commandSubscriptions.Add
    override __.UnsubscribeFromCommands() = commandSubscriptions.Clear()
    member __.Title with get() = LocalisedStrings.AppTitle
    member __.SearchForAddressCommand with get() = searchForAddressCommand
    member this.SearchTerm with get() = searchTerm and set(value) = this.RaiseAndSetIfChanged(&searchTerm, value, "SearchTerm") |> ignore
    member this.Location with get() = location and set(value) = this.RaiseAndSetIfChanged(&location, value, "Location") |> ignore
    member __.Markers with get() = markers
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
