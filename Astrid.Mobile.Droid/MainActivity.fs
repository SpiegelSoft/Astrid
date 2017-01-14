namespace Astrid.Mobile.Droid

open Android.Content.PM
open Android.App

open Xamarin.Forms.Platform.Android
open Xamarin.Forms.Maps
open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open ReactiveUI.XamForms
open ReactiveUI

open Astrid.Mobile.Shared
open Xamarin.Forms.Maps.Android

type XamarinForms = Xamarin.Forms.Forms

type DroidPlatform() =
    let geocoder = new Geocoder()
    interface IAstridPlatform with
        member __.GetMainPage() = new RoutedViewHost() :> Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.Geocoder = geocoder

type GeographicMapRenderer() = inherit MapRenderer()

[<assembly: ExportRendererAttribute (typeof<GeographicMap>, typeof<GeographicMapRenderer>)>] do ()
[<Activity (Label = "Astrid.Mobile.Droid", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity () =
    inherit FormsApplicationActivity ()
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        XamarinForms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let application = new App<IAstridPlatform>(new DroidPlatform() :> IAstridPlatform, new UiContext(this), new Configuration(), fun () -> new DashboardViewModel() :> IRoutableViewModel)
        this.LoadApplication application
        let y = Xamarin.Forms.Forms.Context
        y |> ignore
