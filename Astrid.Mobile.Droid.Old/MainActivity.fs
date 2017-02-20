namespace Astrid.Mobile.Droid

open System.IO
open System

open Android.Content.PM
open Android.App

open Xamarin.Forms.Platform.Android
open Xamarin.Forms.Maps
open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open SQLite.Net.Platform.XamarinAndroid
open SQLite.Net

open ReactiveUI.XamForms
open ReactiveUI

open Astrid.Mobile.Shared

type XamarinForms = Xamarin.Forms.Forms

type DroidPlatform() =
    let geocoder = new Geocoder()
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(appFolderPath, fileName)
    let repositoryPath = localFilePath "AstridRepository"
    let placesOfInterest = new PlaceOfInterestRepository(new SQLitePlatformAndroid(), new SQLiteConnectionString(repositoryPath, true))
    interface IAstridPlatform with
        member __.GetMainPage() = new RoutedViewHost() :> Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.Geocoder = geocoder
        member __.PlacesOfInterest = placesOfInterest
        member __.GetLocalFilePath fileName = localFilePath fileName

[<assembly: ExportRendererAttribute (typeof<DashboardMap>, typeof<GeographicMapRenderer>)>] do ()
[<Activity (Label = "Astrid.Mobile.Droid", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() =
    inherit FormsApplicationActivity()
    override this.OnCreate (bundle) =
        base.OnCreate(bundle)
        XamarinForms.Init(this, bundle)
        Xamarin.FormsMaps.Init(this, bundle)
        let application = new App<IAstridPlatform>(new DroidPlatform() :> IAstridPlatform, new UiContext(this), new Configuration(), fun() -> new DashboardViewModel() :> IRoutableViewModel)
        this.LoadApplication application
