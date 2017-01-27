namespace Astrid.Mobile.Droid

open System.Reactive.Disposables
open System.Reactive.Linq
open System

open Xamarin.Forms.Maps.Android
open Xamarin.Forms.Maps

open XamarinForms.Reactive.FSharp

open Android.Gms.Maps.Model
open Android.Gms.Maps
open Android.Runtime

open GeographicLib

open ReactiveUI

type GeographicMapRenderer() =
    inherit MapRenderer()
    let mutable formsMap = Unchecked.defaultof<GeographicMap>
    let mutable googleMap = Unchecked.defaultof<GoogleMap>
    let subscriptions = new CompositeDisposable()
    let infoWindowClicked _ eventArgs = eventArgs |> ignore
    let infoWindowEventHandler = new EventHandler<Android.Gms.Maps.GoogleMap.InfoWindowClickEventArgs>(infoWindowClicked)
    override this.OnElementChanged e =
        base.OnElementChanged(e)
        match box e.OldElement with
        | null -> e |> ignore
        | _ -> googleMap.InfoWindowClick.RemoveHandler infoWindowEventHandler
        match box e.NewElement with
        | null -> e |> ignore
        | _ -> formsMap <- e.NewElement :?> GeographicMap; base.Control.GetMapAsync(this)
    override __.Dispose(disposing) = if disposing then subscriptions.Clear()
    interface IOnMapReadyCallback with 
        member this.OnMapReady map =
            googleMap <- map
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
            googleMap.InfoWindowClick.AddHandler infoWindowEventHandler
            googleMap.SetInfoWindowAdapter this
    interface Android.Gms.Maps.GoogleMap.IInfoWindowAdapter with
        member __.GetInfoContents(marker: Marker): Android.Views.View = failwith "Not implemented yet"
        member __.GetInfoWindow(marker: Marker): Android.Views.View = failwith "Not implemented yet"
    interface IJavaObject with member __.Handle = base.Handle
    interface IDisposable with member __.Dispose() = base.Dispose()
