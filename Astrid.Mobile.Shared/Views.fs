namespace Astrid.Mobile.Shared

open System

open XamarinForms.Reactive.FSharp.ViewHelpers
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms.Maps
open Xamarin.Forms

open Astrid.Localisation

module PinConversion =
    let toPin (marker: MapMarker) =
        match marker with
        | SearchResult result -> new Pin(Type = PinType.SearchResult, Address = result.SearchedForAddress, Position = (result.Location |> XamarinGeographic.position), Label = "Search Result")
        | PlaceOfInterest poi -> new Pin(Type = PinType.Place, Address = String.Join(Environment.NewLine, poi.Address), Position = (poi.Location |> XamarinGeographic.position))

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
                            |> withSearchCommand this.ViewModel.SearchForAddress
                    |])
                theme.GenerateMap(fun m -> this.Map <- m)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Location @>, <@ fun (v:DashboardView) -> (v.Map: GeographicMap).Center @>, id, id)
                    |> withPinBinding(this.ViewModel.Markers, this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Markers @>, <@ fun (v:DashboardView) -> (v.Map: GeographicMap).PinnedLocations @>, PinConversion.toPin)
            |]) |> createFromColumns :> View
    member val AddressSearchBar = Unchecked.defaultof<SearchBar> with get, set
    member val Title = Unchecked.defaultof<Label> with get, set
    member val Map = Unchecked.defaultof<GeographicMap> with get, set
