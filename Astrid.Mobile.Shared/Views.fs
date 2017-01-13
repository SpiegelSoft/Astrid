namespace Astrid.Mobile.Shared

open XamarinForms.Reactive.FSharp.ViewHelpers
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms.Maps
open Xamarin.Forms

open GeographicLib

open Astrid.Localisation

type DashboardView(theme: Theme) as this = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(Themes.AstridTheme)
    override __.CreateContent() =
        theme.GenerateGrid([|"100"; "*"|], [|"*"|]) |> withColumn(
            [|
                theme.VerticalLayout() |> withBlocks(
                    [|
                        theme.GenerateLabel(fun l -> this.Title <- l) 
                            |> withAlignment LayoutOptions.Center LayoutOptions.Center
                            |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Title @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>)
                        theme.GenerateSearchBar(fun sb -> this.AddressSearchBar <- sb)
                            |> withSearchBarPlaceholder LocalisedStrings.SearchForAPlaceOfInterest
                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.SearchAddress @>, <@ fun (v: DashboardView) -> (v.AddressSearchBar: SearchBar).Text @>)
                            |> withSearchCommand this.ViewModel.SearchForAddress
                    |])
                theme.GenerateMap(new GeodesicLocation(51.4<deg>, 0.0<deg>), 4.0<km>, fun m -> this.Map <- m)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.LatitudeDegrees @>, <@ fun (v:DashboardView) -> (v.Map: Map).VisibleRegion.LatitudeDegrees @>)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.LongitudeDegrees @>, <@ fun (v:DashboardView) -> (v.Map: Map).VisibleRegion.LongitudeDegrees @>)
            |]) |> createFromColumns :> View
    member val AddressSearchBar = Unchecked.defaultof<SearchBar> with get, set
    member val Title = Unchecked.defaultof<Label> with get, set
    member val Map = Unchecked.defaultof<Map> with get, set
