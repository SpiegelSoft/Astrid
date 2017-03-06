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
    do base.Content <- 
//        theme.GenerateLabel() |> withContent("Hello") |> withBackgroundColor(Color.FromRgb(0, 59, 111))
//        theme.GenerateLabel() |> withBackgroundColor(Color.Green) |> with
//            |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: MarkerViewModel) -> vm.Details @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>, id)
        theme.GenerateGrid([|"*"; "*"|], [|"*"; "*"|]) |> withColumn(
            [|
                theme.GenerateLabel() |> withBackgroundColor(astridBlue) |> withContent("Hello") |> withWidthRequest 1200.0 |> withHeightRequest 100.0
                theme.GenerateLabel() |> withBackgroundColor(astridBlue) |> withContent("World") |> withWidthRequest 1200.0 |> withHeightRequest 100.0
            |]) |> thenColumn(
            [|
                theme.GenerateLabel() |> withBackgroundColor(astridBlue) |> withContent("Foo") |> withWidthRequest 200.0 |> withHeightRequest 20.0
                theme.GenerateLabel() |> withBackgroundColor(astridBlue) |> withContent("Bar") |> withWidthRequest 200.0 |> withHeightRequest 20.0
            |]) |> createFromColumns
//        new Frame(Content = content)
    new() = new MarkerView(Themes.AstridTheme)

type DashboardView(theme: Theme) as this = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(Themes.AstridTheme)
    override __.CreateContent() =
        theme.GenerateGrid([|"Auto"; "*"|], [|"*"|]) |> withColumn(
            [|
                theme.VerticalLayout() |> withBlocks(
                    [|
                        theme.GenerateLabel(fun l -> this.Title <- l) 
                            |> withAlignment LayoutOptions.Center LayoutOptions.Center
                            |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Title @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>, id)
                        theme.GenerateSearchBar(fun sb -> this.AddressSearchBar <- sb)
                            |> withSearchBarPlaceholder LocalisedStrings.SearchForAPlaceOfInterest
                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.SearchAddress @>, <@ fun (v: DashboardView) -> (v.AddressSearchBar: SearchBar).Text @>, id, id)
                            |> withSearchCommand this.ViewModel.SearchForAddressCommand
                    |])
                theme.GenerateMap<MarkedLocation>(fun m -> this.Map <- m)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Location @>, <@ fun (v:DashboardView) -> (v.Map: GeographicMap<MarkedLocation>).Center @>, id, id)
                    |> withPinBinding(this.ViewModel.Markers, PinConversion.toPin)
            |]) |> createFromColumns :> View
    member val AddressSearchBar = Unchecked.defaultof<SearchBar> with get, set
    member val Title = Unchecked.defaultof<Label> with get, set
    member val Map = Unchecked.defaultof<GeographicMap<MarkedLocation>> with get, set
