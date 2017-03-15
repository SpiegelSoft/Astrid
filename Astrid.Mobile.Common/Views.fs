namespace Astrid.Mobile.Common

open System

open XamarinForms.Reactive.FSharp.ViewHelpers
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms.Maps
open Xamarin.Forms

open Astrid.Localisation

type MarkedLocation(viewModel: MarkerViewModel) =
    inherit GeographicPin(viewModel.Location)
    member val ViewModel = viewModel

module PinConversion = let toPin (marker: MarkerViewModel) = new MarkedLocation(marker)

type MarkerView(theme: Theme) as this =
    inherit ContentView<MarkerViewModel>(theme)
    let astridBlue = Color.FromRgb(0, 59, 111)
    override __.CreateContent() =
        theme.VerticalLayout()
            |> withBlocks(
                [|
                    theme.GenerateLabel(fun l -> this.Title <- l)
                        |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: MarkerViewModel) -> vm.Text @>, <@ fun (v: MarkerView) -> (v.Title: Label).Text @>, id)
                        |> withHeightRequest 200.0 |> withWidthRequest 480.0 |> withBackgroundColor(astridBlue)
                |]) |> withHeightRequest 212.0 |> withWidthRequest 492.0 |> withBackgroundColor astridBlue :> View
    new() = new MarkerView(Themes.AstridTheme)
    member val Title = Unchecked.defaultof<Label> with get, set

type GeocodingResultView(theme: Theme) as this =
    inherit ContentPage<GeocodingResultViewModel, GeocodingResultView>(theme)
    override __.CreateContent() =
        theme.VerticalLayout() |> withBlocks(
            [|
                theme.GenerateBoxView() |> withHorizontalOptions LayoutOptions.FillAndExpand |> withHeightRequest 10.0
                theme.GenerateTitle() |> withLabelText(LocalisedStrings.SearchResult) 
                    |> withAlignment LayoutOptions.Center LayoutOptions.Center
                theme.GenerateSubtitle(fun l -> this.Subtitle <- l) 
                    |> withAlignment LayoutOptions.Center LayoutOptions.Center
                    |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> vm.Headline @>, <@ fun (v: GeocodingResultView) -> (v.Subtitle: Label).Text @>, id)
                theme.GenerateButton(fun b -> this.CreatePlaceOfInterestButton <- b)
                    |> withAlignment LayoutOptions.Center LayoutOptions.Center
                    |> withCaption(LocalisedStrings.CreatePlaceOfInterest)
                    |> withCommandBinding (this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> vm.ShowPlaceOfInterestCreationForm @>, <@ fun (v: GeocodingResultView) -> v.CreatePlaceOfInterestButton @>)
                theme.VerticalLayout(fun l -> this.PlaceOfInterestCreationForm <- l)
                    |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> vm.CreatingPlaceOfInterest @>, <@ fun (v: GeocodingResultView) -> (v.PlaceOfInterestCreationForm: StackLayout).IsVisible @>, id)
                    |> withBlocks(
                        [|
                            theme.GenerateBoxView() |> withHorizontalOptions LayoutOptions.FillAndExpand |> withHeightRequest 1.0
                            theme.GenerateTitle() |> withLabelText(LocalisedStrings.NewPlaceOfInterest) |> withAlignment LayoutOptions.Center LayoutOptions.Center
                            theme.GenerateScrollView() |> withContent(
                                theme.GenerateGrid(["*"; "*"; "Auto"; "*"], ["2*"; "5*"]) |> withRow(
                                    [|
                                        theme.GenerateLabel() |> withLabelText(LocalisedStrings.Title)
                                        theme.GenerateEntry(fun e -> this.NewPlaceOfInterestTitle <- e)
                                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> (vm.PlaceOfInterestCreation: CreatePlaceOfInterestViewModel).Title @>, <@ fun (v: GeocodingResultView) -> (v.NewPlaceOfInterestTitle: Entry).Text @>, id, id)
                                    |]) |> thenRow(
                                    [|
                                        theme.GenerateLabel() |> withLabelText(LocalisedStrings.Description)
                                        theme.GenerateEntry(fun e -> this.NewPlaceOfInterestDescription <- e)
                                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> (vm.PlaceOfInterestCreation: CreatePlaceOfInterestViewModel).Description @>, <@ fun (v: GeocodingResultView) -> (v.NewPlaceOfInterestDescription: Entry).Text @>, id, id)
                                    |]) |> thenRow(
                                    [|
                                        theme.GenerateLabel() |> withLabelText(LocalisedStrings.Address)
                                        theme.GenerateEditor(fun e -> this.NewPlaceOfInterestAddress <- e)
                                            |> withEditorFontSize 14.0
                                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> (vm.PlaceOfInterestCreation: CreatePlaceOfInterestViewModel).Address @>, <@ fun (v: GeocodingResultView) -> (v.NewPlaceOfInterestAddress: Editor).Text @>, id, id)
                                    |]) |> thenRow(
                                    [|
                                        theme.GenerateButton(fun b -> this.SavePlaceOfInterestButton <- b) 
                                            |> withColumnSpan(2)
                                            |> withCaption LocalisedStrings.Save 
                                            |> withAlignment LayoutOptions.End LayoutOptions.Center
                                            |> withCommandBinding (this.ViewModel, this, <@ fun (vm: GeocodingResultViewModel) -> vm.CreatePlaceOfInterest @>, <@ fun (v: GeocodingResultView) -> v.SavePlaceOfInterestButton @>)
                                    |]) |> createFromRows |> withPadding(new Thickness(20.0)) :> View)
                        |])
           |]) :> View
    new() = new GeocodingResultView(Themes.AstridTheme)
    member val Subtitle = Unchecked.defaultof<Label> with get, set
    member val CreatePlaceOfInterestButton = Unchecked.defaultof<Button> with get, set
    member val SavePlaceOfInterestButton = Unchecked.defaultof<Button> with get, set
    member val PlaceOfInterestCreationForm = Unchecked.defaultof<StackLayout> with get, set
    member val NewPlaceOfInterestDescription = Unchecked.defaultof<Entry> with get, set
    member val NewPlaceOfInterestTitle = Unchecked.defaultof<Entry> with get, set
    member val NewPlaceOfInterestAddress = Unchecked.defaultof<Editor> with get, set

type DashboardView(theme: Theme) as this = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(Themes.AstridTheme)
    override __.CreateContent() =
        theme.GenerateGrid([|"Auto"; "*"|], [|"*"|]) |> withColumn(
            [|
                theme.VerticalLayout() |> withBlocks(
                    [|
                        theme.GenerateLabel(fun l -> this.Title <- l) 
                            |> withAlignment LayoutOptions.Center LayoutOptions.End
                            |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Title @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>, id)
                        theme.GenerateSearchBar(fun sb -> this.AddressSearchBar <- sb)
                            |> withSearchBarPlaceholder LocalisedStrings.SearchForAPlaceOfInterest
                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.SearchTerm @>, <@ fun (v: DashboardView) -> (v.AddressSearchBar: SearchBar).Text @>, id, id)
                            |> withSearchCommand this.ViewModel.SearchForAddressCommand
                    |])
                theme.GenerateMap<MarkedLocation>(fun m -> this.Map <- m)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Location @>, <@ fun (v:DashboardView) -> (v.Map: GeographicMap<MarkedLocation>).Center @>, id, id)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Radius @>, <@ fun (v:DashboardView) -> (v.Map: GeographicMap<MarkedLocation>).Radius @>, id, id)
                    |> withPinBinding(this.ViewModel.Markers, PinConversion.toPin)
            |]) |> createFromColumns :> View
    member val AddressSearchBar = Unchecked.defaultof<SearchBar> with get, set
    member val Title = Unchecked.defaultof<Label> with get, set
    member val Map = Unchecked.defaultof<GeographicMap<MarkedLocation>> with get, set
