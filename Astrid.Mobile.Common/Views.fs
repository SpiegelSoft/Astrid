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
