namespace Astrid.Mobile.Shared

open ReactiveUI

open Xamarin.Forms.FSharp

open LocatorDefaults

type DashboardViewModel(?host: IScreen) =
    inherit ReactiveViewModel()
    let host = LocateIfNone host
    member __.Title with get() = "Dashboard"
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"

