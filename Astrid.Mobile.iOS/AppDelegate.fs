namespace Astrid.Mobile.iOS

open SQLite.Net.Platform.XamarinIOS
open SQLite.Net

open XamarinForms.Reactive.FSharp

open Xamarin.Forms.Platform.iOS
open Xamarin.Forms.Maps

open ReactiveUI.XamForms
open ReactiveUI

open System.IO
open System

open UIKit

open Foundation

open Astrid.Mobile.Common

type XamarinForms = Xamarin.Forms.Forms

type IosPlatform() =
    let geocoder = new Geocoder()
    static let docFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    static let libFolder = Path.Combine(docFolder, "..", "Library", "Databases")
    do if not (Directory.Exists(libFolder)) then Directory.CreateDirectory(libFolder) |> ignore
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(libFolder, fileName)
    let repositoryPath = localFilePath "AstridRepository"
    let placesOfInterest = new PlaceOfInterestRepository(new SQLitePlatformIOS(), new SQLiteConnectionString(repositoryPath, true))
    interface IAstridPlatform with
        member __.GetMainPage() = new ReactiveUI.XamForms.RoutedViewHost() :> Xamarin.Forms.Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.Geocoder = geocoder
        member __.PlacesOfInterest = placesOfInterest
        member __.GetLocalFilePath fileName = localFilePath fileName

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit FormsApplicationDelegate ()
    override this.FinishedLaunching (app, options) =
        XamarinForms.Init()
        this.LoadApplication(new App<IAstridPlatform>(new IosPlatform() :> IAstridPlatform, new UiContext(this), new Configuration(), fun() -> new DashboardViewModel() :> IRoutableViewModel))
        base.FinishedLaunching(app, options)