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

module PinConversion =
    let toPin (marker: MarkerViewModel) = new MarkedLocation(marker)

type MarkerInfoWindow(theme: Theme) =
    inherit ContentView<MarkerViewModel>(theme)
    do base.Content <- 
        let content = 
            theme.GenerateGrid([|"*"; "*"|], [|"*"; "*"|]) |> withColumn(
                [|
                    theme.GenerateLabel() |> withBackgroundColor(Color.Aqua) |> withContent("Hello")
                    theme.GenerateLabel() |> withBackgroundColor(Color.Aqua) |> withContent("World")
                |]) |> thenColumn(
                [|
                    theme.GenerateLabel() |> withBackgroundColor(Color.Aqua) |> withContent("Foo")
                    theme.GenerateLabel() |> withBackgroundColor(Color.Aqua) |> withContent("Bar")
                |]) |> createFromColumns |> withBackgroundColor(Color.Yellow)  :> View
        new Frame(Content = content)
    new() = new MarkerInfoWindow(Themes.AstridTheme)

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
