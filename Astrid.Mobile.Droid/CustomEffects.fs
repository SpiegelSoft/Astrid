namespace Astrid.Mobile.Droid

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open Xamarin.Forms.Platform.Android
open Xamarin.Forms.Maps
open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open Android.Gms.Maps.Model
open Android.Gms.Maps

open GeographicLib

open Splat

open ReactiveUI

type ILoadGoogleMap = abstract member MapLoaded: MapView -> IObservable<GoogleMap>

type PinnedLocationEffect() =
    inherit PlatformEffect()
    let subscriptions = new CompositeDisposable()
    let mutable formsMap = Unchecked.defaultof<GeographicMap>
    let mutable androidMapView = Unchecked.defaultof<MapView>
    let subscribeToPinUpdates (googleMap: GoogleMap) =
        let pinsUpdated _ = 
            googleMap.Clear()
            for pin in formsMap.PinnedLocations do 
                let marker = new MarkerOptions()
                match pin.PinType with
                | PinType.SearchResult -> marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed)) |> ignore
                | PinType.Place -> marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure)) |> ignore
                | _ -> pin |> ignore
                marker.SetPosition(new LatLng(pin.Location.Latitude / 1.0<deg>, pin.Location.Longitude / 1.0<deg>)) |> ignore
                googleMap.AddMarker marker |> ignore
        formsMap.PinnedLocations.ItemsAdded.ObserveOn(RxApp.MainThreadScheduler).Subscribe(pinsUpdated) |> subscriptions.Add
        formsMap.PinnedLocations.ItemsRemoved.ObserveOn(RxApp.MainThreadScheduler).Subscribe(pinsUpdated) |> subscriptions.Add
    override this.OnAttached() = 
        formsMap <- this.Element :?> GeographicMap; androidMapView <- this.Control :?> MapView
        match box formsMap with 
        | null -> this.Element.GetType().Name |> sprintf "The PinnedLocation effect can only be added to a GeographicMap object. It has been added to an object of type %s" |> invalidOp 
        | _ -> formsMap |> ignore
        match box androidMapView with
        | null -> invalidOp "The underlying map is not an Android map. This should never happen."
        | _ -> androidMapView |> ignore
        let mapLoadedObservable = Locator.Current.GetService<IUiContext>().Context :?> ILoadGoogleMap
        subscribeToPinUpdates |> mapLoadedObservable.MapLoaded(androidMapView).Subscribe |> subscriptions.Add
    override __.OnDetached() = 
        subscriptions.Clear()

[<assembly: ResolutionGroupName("Astrid")>]
[<assembly: ExportEffect(typeof<PinnedLocationEffect>, "PinnedLocationEffect")>]
()
