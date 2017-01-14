namespace Astrid.Mobile.Shared

open XamarinForms.Reactive.FSharp.ViewHelpers
open XamarinForms.Reactive.FSharp.Themes
open XamarinForms.Reactive.FSharp

open Xamarin.Forms

open GeographicLib

open Astrid.Localisation

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
                            |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Title @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>)
                        theme.GenerateSearchBar(fun sb -> this.AddressSearchBar <- sb)
                            |> withSearchBarPlaceholder LocalisedStrings.SearchForAPlaceOfInterest
                            |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.SearchAddress @>, <@ fun (v: DashboardView) -> (v.AddressSearchBar: SearchBar).Text @>)
                            |> withSearchCommand this.ViewModel.SearchForAddress
                    |])
                theme.GenerateMap(fun m -> this.Map <- m)
                    |> withTwoWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Location @>, <@ fun (v:DashboardView) -> (v.Map: GeographicMap).Center @>)
            |]) |> createFromColumns :> View
    member val AddressSearchBar = Unchecked.defaultof<SearchBar> with get, set
    member val Title = Unchecked.defaultof<Label> with get, set
    member val Map = Unchecked.defaultof<GeographicMap> with get, set
