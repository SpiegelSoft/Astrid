namespace Astrid.Mobile.Shared

open Xamarin.Forms.FSharp.ViewHelpers
open Xamarin.Forms.FSharp.Themes
open Xamarin.Forms.FSharp
open Xamarin.Forms

type DashboardView(theme: Theme) as this = 
    inherit ContentPage<DashboardViewModel, DashboardView>(theme)
    let initialise() =
        this.Content <- theme.GenerateGrid([|"*"|], [|"*"|]) |> withColumn(
            [|
                theme.GenerateLabel() |> withAlignment LayoutOptions.Center LayoutOptions.Center |> withSetUpAction(fun l -> this.Title <- l)
                    |> withOneWayBinding(this.ViewModel, this, <@ fun (vm: DashboardViewModel) -> vm.Title @>, <@ fun (v: DashboardView) -> (v.Title: Label).Text @>)
            |]) |> createFromColumns
    new() = new DashboardView(Themes.AstridTheme)
    override __.OnAppearing() =
        base.OnAppearing()
        match box this.Content with
        | null -> initialise()
        | _ -> this |> ignore

    member val Title = Placeholders.Label with get, set


