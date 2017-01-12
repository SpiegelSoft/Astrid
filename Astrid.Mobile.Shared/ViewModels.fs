namespace Astrid.Mobile.Shared

open ReactiveUI

open XamarinForms.Reactive.FSharp

open LocatorDefaults

open GeographicLib

type DashboardViewModel(?host: IScreen) =
    inherit ReactiveViewModel()
    let host = LocateIfNone host
    let mutable location = new GeodesicLocation()
    member __.Title with get() = "Astrid"
    member __.LatitudeDegrees with get() = location.Latitude / 1.0<deg> and set(value) = location <- new GeodesicLocation(value * 1.0<deg>, location.Longitude)
    member __.LongitudeDegrees with get() = location.Longitude / 1.0<deg> and set(value) = location <- new GeodesicLocation(location.Latitude, value * 1.0<deg>)
    interface IRoutableViewModel with
        member __.HostScreen = host
        member __.UrlPathSegment = "Dashboard"
