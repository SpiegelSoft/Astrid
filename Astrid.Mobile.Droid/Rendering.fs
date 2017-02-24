namespace Astrid.Mobile.Droid

open System.Reactive.Disposables
open System.Collections.Generic
open System.Reactive.Linq
open System

open Xamarin.Forms.Platform.Android
open Xamarin.Forms.Maps.Android

open XamarinForms.Reactive.FSharp

open Android.Gms.Maps.Model
open Android.Gms.Maps
open Android.Runtime

open GeographicLib

open ReactiveUI

open Astrid.Mobile.Common

type DashboardMap = GeographicMap<MarkedLocation>

type GeographicMapRenderer() =
    inherit MapRenderer()
    let mutable formsMap = Unchecked.defaultof<DashboardMap>
    let mutable googleMap = Unchecked.defaultof<GoogleMap>
    let markerViewModel = new Dictionary<string, MarkerViewModel>()
    let subscriptions = new CompositeDisposable()
    let infoWindowClicked _ (eventArgs: GoogleMap.InfoWindowClickEventArgs) = 
        eventArgs |> ignore
    let infoWindowEventHandler = new EventHandler<Android.Gms.Maps.GoogleMap.InfoWindowClickEventArgs>(infoWindowClicked)
    override this.OnElementChanged e =
        base.OnElementChanged(e)
        match box e.OldElement with
        | null -> e |> ignore
        | _ -> googleMap.InfoWindowClick.RemoveHandler infoWindowEventHandler
        match box e.NewElement with
        | null -> e |> ignore
        | _ -> formsMap <- e.NewElement :?> DashboardMap; base.Control.GetMapAsync(this)
    override __.Dispose(disposing) = if disposing then subscriptions.Clear()
    interface IOnMapReadyCallback with 
        member this.OnMapReady map =
            googleMap <- map
            let pinsUpdated _ = 
                googleMap.Clear()
                markerViewModel.Clear()
                for pin in formsMap.PinnedLocations do 
                    let marker = new MarkerOptions()
                    match pin.ViewModel.Details with
                    | SearchResult _ -> marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed)) |> ignore
                    | PlaceOfInterest _ -> marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure)) |> ignore
                    marker.SetPosition(new LatLng(pin.Location.Latitude / 1.0<deg>, pin.Location.Longitude / 1.0<deg>)) |> ignore
                    googleMap.AddMarker marker |> fun m -> markerViewModel.[m.Id] <- pin.ViewModel
            formsMap.PinnedLocations.ItemsAdded.ObserveOn(RxApp.MainThreadScheduler).Subscribe(pinsUpdated) |> subscriptions.Add
            formsMap.PinnedLocations.ItemsRemoved.ObserveOn(RxApp.MainThreadScheduler).Subscribe(pinsUpdated) |> subscriptions.Add
            googleMap.InfoWindowClick.AddHandler infoWindowEventHandler
            googleMap.SetInfoWindowAdapter this
    interface Android.Gms.Maps.GoogleMap.IInfoWindowAdapter with
        member this.GetInfoContents(marker: Marker): Android.Views.View = 
            marker.Title <- "Hello"
            marker.Snippet <- "World"
            let view = markerViewModel.[marker.Id] |> ViewLocator.Current.ResolveView :?> MarkerInfoWindow
            let renderer = Platform.CreateRenderer(view.Content)
            let vg = renderer.ViewGroup
            let androidView = vg :> Android.Views.View
            let height, width = androidView.Height, androidView.Width
            renderer.UpdateLayout()
            androidView.SetBackgroundColor(Android.Graphics.Color.Aqua) |> ignore
            let cc = vg.ChildCount
            let child = vg.GetChildAt(0)
            androidView
        member __.GetInfoWindow(marker: Marker): Android.Views.View = Unchecked.defaultof<Android.Views.View>
    interface IJavaObject with member __.Handle = base.Handle
    interface IDisposable with member __.Dispose() = base.Dispose()
