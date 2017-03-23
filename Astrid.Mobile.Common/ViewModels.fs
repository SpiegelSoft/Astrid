namespace Astrid.Mobile.Common

open System.Reactive.Disposables
open System.Collections.Generic
open System.Threading.Tasks
open System.Reactive.Linq
open System.Linq
open System

open ReactiveUI

open XamarinForms.Reactive.FSharp

open Astrid.Localisation

open LocatorDefaults

open GeographicLib

[<StructuralEquality; NoComparison>]
type LocationDetails =
    | GeocodingResult of SearchResult
    | PlaceOfInterest of PlaceOfInterest

type TimelineViewModel(placeOfInterest: PlaceOfInterest, deletePlaceOfInterestCommand: ReactiveCommand<PlaceOfInterest, Reactive.Unit>, ?host: IScreen) =
    inherit PageViewModel()
    let host = LocateIfNone host
    let delete confirmed =
        match confirmed with
        | false -> confirmed |> ignore
        | true -> deletePlaceOfInterestCommand.Execute(placeOfInterest) |> ignore
    let deletePlaceOfInterest (vm: TimelineViewModel) (_: Reactive.Unit) = 
        vm.DisplayConfirmation(
            { 
                Title = LocalisedStrings.DeleteLocation
                Message = String.Format(LocalisedStrings.DoYouReallyWishToDeleteTheLocation, placeOfInterest.Label)
                Accept = LocalisedStrings.Yes
                Decline = LocalisedStrings.No 
            }).FirstAsync().Subscribe(delete) |> ignore
        Task.FromResult(true)
    member val DeletePlaceOfInterest = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    override this.SetUpCommands() = 
        this.DeletePlaceOfInterest <- deletePlaceOfInterest this |> ReactiveCommand.CreateFromTask |> ObservableExtensions.disposeWith this.PageDisposables
    override this.TearDownCommands() = 
        this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Timeline"

type CreatePlaceOfInterestViewModel() =
    inherit ReactiveObject()
    let mutable title = String.Empty
    let mutable description = String.Empty
    let mutable address = String.Empty
    member this.Title with get() = title and set(value) = this.RaiseAndSetIfChanged(&title, value, "Title") |> ignore
    member this.Description with get() = description and set(value) = this.RaiseAndSetIfChanged(&description, value, "Description") |> ignore
    member this.Address with get() = address and set(value) = this.RaiseAndSetIfChanged(&address, value, "Address") |> ignore
    member this.PlaceOfInterest() =
        {
            PlaceOfInterestId = 0
            Label = this.Title
            Description = match String.IsNullOrWhiteSpace(this.Description) with | false -> Some this.Description | true -> None 
            Image = PlaceholderImage
            Address = this.Address.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries) |> Array.ofSeq
        }
    member this.CanSave() =
        (String.IsNullOrEmpty(this.Title) |> not)
        && (String.IsNullOrEmpty(this.Address) |> not)
        && (this.Address.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries).Count() > 1)

open ExpressionConversion
type GeocodingResultViewModel(location, placeOfInterest: PlaceOfInterest, convertToPlaceOfInterestCommand: ReactiveCommand<PlaceOfInterest, PlaceOfInterest>, ?host: IScreen, ?platform: IAstridPlatform) =
    inherit PageViewModel()
    let mutable creatingPlaceOfInterest = false
    let host, platform = LocateIfNone host, LocateIfNone platform
    let showPlaceOfInterestCreationForm (_: Reactive.Unit) = async { return true } |> Async.StartAsTask
    let showForm (vm: GeocodingResultViewModel) visible = vm.CreatingPlaceOfInterest <- visible
    let closeForm success = host.Router.NavigateBack.Execute() |> ignore
    let placeOfInterestCreation = new CreatePlaceOfInterestViewModel()
    let createPlaceOfInterest (_: Reactive.Unit) = 
        async {
            let newPlaceOfInterest = placeOfInterestCreation.PlaceOfInterest()
            do! platform.PlacesOfInterest.AddPlaceOfInterestAsync(newPlaceOfInterest, location)
            convertToPlaceOfInterestCommand.Execute(newPlaceOfInterest) |> ObservableExtensions.ignoreOnce
            return true 
        } |> Async.StartAsTask
    do placeOfInterestCreation.Title <- placeOfInterest.Label; placeOfInterestCreation.Address <- placeOfInterest.Address.[0]
    member val Headline = placeOfInterest.Label
    member this.CreatingPlaceOfInterest with get() = creatingPlaceOfInterest and set(value) = this.RaiseAndSetIfChanged(&creatingPlaceOfInterest, value, "CreatingPlaceOfInterest") |> ignore
    member val ShowPlaceOfInterestCreationForm = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    member val CreatePlaceOfInterest = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, bool>> with get, set
    member val PlaceOfInterestCreation = placeOfInterestCreation
    override this.SetUpCommands() = 
        this.CreatePlaceOfInterest <- 
            ReactiveCommand.CreateFromTask(createPlaceOfInterest, 
                placeOfInterestCreation.Changed.Select(fun e -> e.Sender :> obj).OfType<CreatePlaceOfInterestViewModel>().Select(fun vm -> vm.CanSave())
                |> Observable.merge (Observable.Return(placeOfInterestCreation.CanSave())))
                |> ObservableExtensions.disposeWith this.PageDisposables
        this.ShowPlaceOfInterestCreationForm <-
            ReactiveCommand.CreateFromTask(showPlaceOfInterestCreationForm,
                this.WhenAnyValue(toLinq <@ fun (vm: GeocodingResultViewModel) -> vm.CreatingPlaceOfInterest @>).Select(fun c -> not c))
                |> ObservableExtensions.disposeWith this.PageDisposables
        this.ShowPlaceOfInterestCreationForm.ObserveOn(RxApp.MainThreadScheduler).Subscribe(showForm this) |> ObservableExtensions.disposeWith this.PageDisposables |> ignore
        this.CreatePlaceOfInterest.ObserveOn(RxApp.MainThreadScheduler).Subscribe(closeForm) |> ObservableExtensions.disposeWith this.PageDisposables |> ignore
    override this.TearDownCommands() = 
        this.PageDisposables.Clear()
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Search Result"

type MarkerViewModel(location, details: LocationDetails, ?host: IScreen) =
    inherit ReactiveObject()
    let host = LocateIfNone host
    let placeOfInterest =
        match details with
        | GeocodingResult result -> { PlaceOfInterestId = 0; Label = result.SearchTerm; Description = None; Image = PlaceholderImage; Address = result.Address }
        | PlaceOfInterest poi -> poi
    let convertToPlaceOfInterest (placeOfInterest: PlaceOfInterest) = Task.FromResult(placeOfInterest)
    let convertToPlaceOfInterestCommand = ReactiveCommand.CreateFromTask convertToPlaceOfInterest
    let deletePlaceOfInterest (placeOfInterest: PlaceOfInterest) = Task.FromResult(0) :> Task
    let deletePlaceOfInterestCommand = ReactiveCommand.CreateFromTask deletePlaceOfInterest
    member val Location = location
    member val PlaceOfInterest = placeOfInterest
    member val Details = details
    member val ConvertToPlaceOfInterestCommand = convertToPlaceOfInterestCommand
    member val DeletePlaceOfInterestCommand = deletePlaceOfInterestCommand
    member this.Text = placeOfInterest.Label
    member this.HeadlineTextExpression() =
        match details with
        | GeocodingResult result -> 
            let searchTerm = result.SearchTerm
            <@ fun (vm: MarkerViewModel) -> searchTerm @>
        | PlaceOfInterest poi -> 
            let label = poi.Label
            <@ fun (vm: MarkerViewModel) -> label @>
    member val Screen = host

type DashboardViewModel(?host: IScreen, ?platform: IAstridPlatform) =
    inherit PageViewModel()
    let host, platform = LocateIfNone host, LocateIfNone platform
    let geocoder = platform.Geocoder
    let markers = new ReactiveList<MarkerViewModel>()
    let geocodingMarkers = new ReactiveList<MarkerViewModel * IDisposable>()
    let geocodingSubsriptions = new CompositeDisposable()
    let clearGeocodingResults() =
        geocodingMarkers |> Seq.iter (fun (_, s) -> s.Dispose()); geocodingMarkers.Clear()
        let geocodingResults = markers |> Seq.choose (fun m -> match m.Details with | GeocodingResult _ -> Some m | _ -> None) |> Array.ofSeq
        geocodingResults |> Seq.iter (markers.Remove >> ignore)
    let addGeocodingMarker (marker: MarkerViewModel) = 
        let command = marker.ConvertToPlaceOfInterestCommand
        let subscription = marker.ConvertToPlaceOfInterestCommand.ObserveOn(RxApp.MainThreadScheduler).Subscribe(fun (poi: PlaceOfInterest) -> 
            markers.Add(new MarkerViewModel(marker.Location, PlaceOfInterest poi, host))
            clearGeocodingResults())
        geocodingMarkers.Add(marker, subscription)
        markers.Add(marker)
    let geocodeAddress(vm: DashboardViewModel) (_: Reactive.Unit) =
        clearGeocodingResults()
        async {
            let searchTerm = vm.SearchTerm
            let! positions = geocoder.GetPositionsForAddressAsync(searchTerm) |> Async.AwaitTask
            let! nearby = positions |> Seq.map (fun pos -> geocoder.GetAddressesForPositionAsync(pos).ContinueWith(fun (t: Task<string seq>) -> (pos, t.Result)) |> Async.AwaitTask) |> Async.Parallel
            let poiMarkers = markers |> Seq.choose (fun m -> match m.Details with | PlaceOfInterest _ -> Some m | _ -> None) |> Array.ofSeq
            let geocodingMarkers = 
                nearby
                |> Seq.filter(fun (pos, address) -> poiMarkers |> Seq.exists (fun m -> m.Location = XamarinGeographic.geodesicLocation pos) |> not) 
                |> Seq.map (fun (pos, address) -> new MarkerViewModel(XamarinGeographic.geodesicLocation pos, GeocodingResult { SearchTerm = searchTerm; Address = address |> Array.ofSeq }))
                |> Array.ofSeq
            geocodingMarkers |> Seq.iter addGeocodingMarker
            return geocodingMarkers
        } |> Async.StartAsTask
    let initialisePage (vm: DashboardViewModel) (_:Reactive.Unit) =
        clearGeocodingResults(); markers.Clear()
        async {
            let! placesOfInterest = platform.PlacesOfInterest.GetAllPlacesOfInterestAsync()
            let poiMarkers = placesOfInterest |> Seq.map (fun (poi, location) -> new MarkerViewModel(location, PlaceOfInterest poi)) |> Array.ofSeq
            poiMarkers |> Seq.iter markers.Add
            return poiMarkers
        } |> Async.StartAsTask
    let showResults (vm: DashboardViewModel) (results: MarkerViewModel[]) = 
        let radius, centre = results |> Array.map (fun m -> m.Location) |> GeographicMapScaling.scaleToMarkers
        vm.Location <- centre
        vm.Radius <- radius
    let displayEmptySetMessage (vm: DashboardViewModel) (_: MarkerViewModel[]) =
        vm.DisplayAlertMessage({ Title = LocalisedStrings.NoResultsFound; Message = String.Format(LocalisedStrings.NoResultsFoundForAddress, vm.SearchTerm); Accept = LocalisedStrings.OK }) |> ignore
    let mutable searchTerm = String.Empty
    let mutable location = new GeodesicLocation(51.49996<deg>, -0.13663<deg>)
    let mutable radius = 2.0<km>
    override this.SetUpCommands() = 
        this.SearchForAddressCommand <- geocodeAddress this |> ReactiveCommand.CreateFromTask |> ObservableExtensions.disposeWith this.PageDisposables
        this.InitialisePageCommand <- initialisePage this |> ReactiveCommand.CreateFromTask |> ObservableExtensions.disposeWith this.PageDisposables
        (this.SearchForAddressCommand :> IObservable<MarkerViewModel[]>).Where(Seq.isEmpty).ObserveOn(RxApp.MainThreadScheduler).Subscribe(displayEmptySetMessage this) |> ObservableExtensions.disposeWith this.PageDisposables |> ignore
        (this.SearchForAddressCommand :> IObservable<MarkerViewModel[]>).Where(Seq.isEmpty >> not).ObserveOn(RxApp.MainThreadScheduler).Subscribe(showResults this) |> ObservableExtensions.disposeWith this.PageDisposables |> ignore
        (this.InitialisePageCommand :> IObservable<MarkerViewModel[]>).Where(Seq.isEmpty >> not).ObserveOn(RxApp.MainThreadScheduler).Subscribe(showResults this) |> ObservableExtensions.disposeWith this.PageDisposables |> ignore
    override this.TearDownCommands() = 
        this.PageDisposables.Clear()
    member __.Title with get() = LocalisedStrings.AppTitle
    member val SearchForAddressCommand = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, MarkerViewModel[]>> with get, set
    member val InitialisePageCommand = Unchecked.defaultof<ReactiveCommand<Reactive.Unit, MarkerViewModel[]>> with get, set
    member this.SearchTerm with get() = searchTerm and set(value) = this.RaiseAndSetIfChanged(&searchTerm, value, "SearchTerm") |> ignore
    member this.Location with get() = location and set(value) = this.RaiseAndSetIfChanged(&location, value, "Location") |> ignore
    member this.Radius with get() = radius and set(value) = this.RaiseAndSetIfChanged(&radius, value, "Radius") |> ignore
    member __.Markers with get() = markers
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
