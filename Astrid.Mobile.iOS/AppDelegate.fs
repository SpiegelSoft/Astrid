namespace Astrid.Mobile.iOS

open Xamarin.Forms.Platform.iOS
open Xamarin.Forms.Maps

open System.IO
open System

open UIKit

open Foundation

open Astrid.Mobile.Shared

type IosPlatform() =
    let geocoder = new Geocoder()
    static let docFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    static let libFolder = Path.Combine(docFolder, "..", "Library", "Databases")
    do if not (Directory.Exists(libFolder)) then Directory.CreateDirectory(libFolder)
    static let appFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    let localFilePath fileName = Path.Combine(libFolder, fileName)
    let repositoryPath = localFilePath "AstridRepository"
    let placesOfInterest = new PlaceOfInterestRepository(new SQLitePlatformAndroid(), new SQLiteConnectionString(repositoryPath, true))
    interface IAstridPlatform with
        member __.GetMainPage() = new RoutedViewHost() :> Page
        member __.RegisterDependencies(_) = 0 |> ignore
        member __.Geocoder = geocoder
        member __.PlacesOfInterest = placesOfInterest
        member __.GetLocalFilePath fileName = localFilePath fileName

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit UIApplicationDelegate ()

    let window = new UIWindow (UIScreen.MainScreen.Bounds)

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =
        // If you have defined a root view controller, set it here:
        // window.RootViewController <- new MyViewController ()
        window.MakeKeyAndVisible ()
        true