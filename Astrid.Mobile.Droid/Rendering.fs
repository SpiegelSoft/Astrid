namespace Astrid.Mobile.Droid

open System.Reactive.Disposables
open System.Collections.Generic
open System.Reactive.Linq
open System

open Xamarin.Forms.Platform.Android
open Xamarin.Forms.Maps.Android
open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open Android.Gms.Maps.Model
open Android.Graphics
open Android.Gms.Maps
open Android.Runtime

open GeographicLib

open ReactiveUI

open Astrid.Mobile.Common

open System.Reflection

type DashboardMap = GeographicMap<MarkedLocation>

module ViewConversion =
    let ToBitmap(viewGroup: Android.Views.ViewGroup, context, width, height) =
        let viewCount = match box viewGroup with | null -> 0 | _ -> viewGroup.ChildCount
        let bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888)
        let layout = new Android.Widget.LinearLayout(context, DrawingCacheEnabled = true)
        let canvas, paint = new Canvas(bitmap), new Paint()
        canvas.DrawBitmap(bitmap, 0.0f, 0.0f, paint)
        for index = 0 to viewCount - 1 do
            let view = viewGroup.GetChildAt(index)
            let width, height = Math.Max(0, view.MeasuredWidth), Math.Max(0, view.MeasuredHeight)
            viewCount |> ignore 
        bitmap

type LayoutRenderer() =
    inherit VisualElementRenderer<View>()
    override this.OnLayout(change, left, top, right, bottom) =
        base.OnLayout(change, left, top, right, bottom)

type GeographicMapRenderer() =
    inherit XamarinForms.Maps.Android.TemporaryPatch.MapRenderer<DashboardMap>()
    let toBitmap(viewGroup: Android.Views.ViewGroup, context, width, height) =
        let viewCount = match box viewGroup with | null -> 0 | _ -> viewGroup.ChildCount
        let bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888)
        let layout = new Android.Widget.LinearLayout(context, DrawingCacheEnabled = true)
        let canvas, paint = new Canvas(bitmap), new Paint()
        let mutable left, top = 10, 10
        canvas.DrawBitmap(bitmap, 0.0f, 0.0f, paint)
        for index = 0 to viewCount - 1 do
            let view = viewGroup.GetChildAt(0)
            viewGroup.RemoveView view
            match view with
            | :? ImageRenderer as imageRenderer -> 
                let src = imageRenderer.Element.Source
                imageRenderer.Layout(0, 0, width, height)
            | :? LabelRenderer as labelRenderer -> 
                view.SetPadding(-10, 0, -10, 0)
                let element = labelRenderer.Element
                layout.AddView (view, int element.WidthRequest, int element.HeightRequest)
                view.Layout(left, top, left + int element.WidthRequest, top + int element.HeightRequest)
            | :? LayoutRenderer as layoutRenderer ->
                layoutRenderer |> ignore
            | _ -> view |> ignore
            top <- top + 100 
        layout.Draw(canvas)
        bitmap
    let mutable formsMap = Unchecked.defaultof<DashboardMap>
    let mutable googleMap = Unchecked.defaultof<GoogleMap>
    let markerViewModel = new Dictionary<string, MarkerViewModel>()
    let subscriptions = new CompositeDisposable()
    let infoWindowClicked _ (eventArgs: GoogleMap.InfoWindowClickEventArgs) = 
        eventArgs |> ignore
    let infoWindowEventHandler = new EventHandler<GoogleMap.InfoWindowClickEventArgs>(infoWindowClicked)
    override this.OnElementChanged e =
        base.OnElementChanged(e)
        match box e.OldElement with
        | null -> e |> ignore
        | _ -> googleMap.InfoWindowClick.RemoveHandler infoWindowEventHandler
        match box e.NewElement with
        | null -> e |> ignore
        | _ -> formsMap <- e.NewElement
    override __.Dispose(disposing) = if disposing then subscriptions.Clear()
    interface IOnMapReadyCallback with 
        override this.OnMapReady map =
            base.OnMapReady map
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
    interface GoogleMap.IInfoWindowAdapter with
        member this.GetInfoContents(marker: Marker): Android.Views.View = 
            let view = markerViewModel.[marker.Id] |> ViewLocator.Current.ResolveView :?> MarkerView
            let density = this.Resources.DisplayMetrics.Density
            let infoWidth = 0.70 * float this.Control.Width
            let infoHeight = (380.0 / 2.8) * float density
            view.WidthRequest <- infoWidth
            view.HeightRequest <- infoHeight
            let renderer = Platform.CreateRenderer(view.Content)
            renderer.SetElement(view.Content)
            let vg = renderer.ViewGroup
            vg.LayoutParameters <- new Android.Views.ViewGroup.LayoutParams (int infoWidth, int infoHeight)
            vg.Layout(0, 0, int infoWidth, int infoHeight)
            vg.DrawingCacheEnabled <- true
            renderer.Tracker.UpdateLayout()
            let image = new Android.Widget.ImageView(this.Context)
            image.SetImageBitmap(toBitmap(vg, this.Context, int view.WidthRequest, int view.HeightRequest))
            image :> Android.Views.View
        member __.GetInfoWindow(marker: Marker): Android.Views.View = Unchecked.defaultof<Android.Views.View>
    interface IJavaObject with member __.Handle = base.Handle
    interface IDisposable with member __.Dispose() = base.Dispose()
