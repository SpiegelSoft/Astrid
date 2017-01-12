namespace Astrid.Mobile.Droid

open Android.Content.PM
open Android.App

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open ReactiveUI.XamForms

open Astrid.Mobile.Shared

type XamarinForms = Xamarin.Forms.Forms

type IAstridPlatform = inherit IPlatform

type DroidPlatform() =
    interface IAstridPlatform with
        member __.GetMainPage() = 
            let host = new RoutedViewHost()
            host :> Page

[<Activity (Label = "Astrid.Mobile.Droid", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity () =
    inherit FormsApplicationActivity ()
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        XamarinForms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let application = new App<IAstridPlatform>(new DroidPlatform() :> IAstridPlatform, new UiContext(this), new Configuration(), new DashboardViewModel())
        this.LoadApplication application
