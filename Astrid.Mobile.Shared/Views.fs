namespace Astrid.Mobile.Shared

open Xamarin.Forms.FSharp.ViewHelpers
open Xamarin.Forms.FSharp.Themes
open Xamarin.Forms.FSharp
open Xamarin.Forms

type DashboardView(theme: Theme) as this = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    new() = new DashboardView(Themes.AstridTheme)
    override __.CreateContent() =
        theme.GenerateGrid([|"*"|], [|"*"|]) |> withColumn(
            [|
                theme.GenerateLabel() |> withAlignment LayoutOptions.Center LayoutOptions.Center |> withSetUpAction(fun l -> this.Title <- l)
                    |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Title @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>)
            |]) |> createFromColumns :> View
    member val Title = Placeholders.Label with get, set


