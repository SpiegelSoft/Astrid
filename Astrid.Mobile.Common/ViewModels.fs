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

open ExpressionConversion
type SearchResultViewModel(placeOfInterest: PlaceOfInterest, ?host: IScreen) as this =
    inherit PageViewModel()
    let mutable creatingPlaceOfInterest = false
    let host = LocateIfNone host
    let commandSubscriptions = new CompositeDisposable()
    let showPlaceOfInterestCreationForm(vm: SearchResultViewModel) = async { return true } |> Async.StartAsTask
    let showForm (vm: SearchResultViewModel) visible = vm.CreatingPlaceOfInterest <- visible
    let showPlaceOfInterestCreationFormCommand = 
        lazy(ReactiveCommand.CreateFromTask(showPlaceOfInterestCreationForm, this.WhenAnyValue(toLinq <@ fun (vm: SearchResultViewModel) -> vm.CreatingPlaceOfInterest @>).Select(fun c -> not c)))
    member val Headline = placeOfInterest.Label
    member __.CreatingPlaceOfInterest with get() = creatingPlaceOfInterest and set(value) = this.RaiseAndSetIfChanged(&creatingPlaceOfInterest, value, "CreatingPlaceOfInterest") |> ignore
    member __.ShowPlaceOfInterestCreationForm = showPlaceOfInterestCreationFormCommand.Force()
    override this.SubscribeToCommands() = this.ShowPlaceOfInterestCreationForm.ObserveOn(RxApp.MainThreadScheduler).Subscribe(showForm this) |> commandSubscriptions.Add
    override __.UnsubscribeFromCommands() = commandSubscriptions.Clear()
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
    let editTimeline() = 
        async {
                host.Router.Navigate.Execute(new SearchResultViewModel(placeOfInterest, host)) |> ignore
                return true
            }
    member __.EditTimelineCommand with get() = ReactiveCommand.CreateFromTask (fun (_: MarkerViewModel) -> editTimeline() |> Async.StartAsTask :> Task)
    member val Location = location
    member val PlaceOfInterest = placeOfInterest
    member val Details = details
    member this.Text = placeOfInterest.Label
    member this.HeadlineTextExpression() =
        match details with
        | SearchResult result -> 
            let searchTerm = result.SearchTerm
            <@ fun (vm: MarkerViewModel) -> searchTerm @>
        | PlaceOfInterest poi -> 
            let label = poi.Label
            <@ fun (vm: MarkerViewModel) -> label @>
    member val Screen = host

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
