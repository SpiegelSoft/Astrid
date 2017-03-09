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
type AndroidViewGroup = Android.Views.ViewGroup
type AndroidView = Android.Views.View
type AndroidColor = Android.Graphics.Color

module ViewConversion =
    let ToAndroidColor (color: Xamarin.Forms.Color) = AndroidColor.Argb(int color.A, int color.R, int color.G, int color.B)

type GeographicMapRenderer() =
    inherit XamarinForms.Maps.Android.TemporaryPatch.MapRenderer<DashboardMap>()
    let toBitmap(viewGroup: AndroidViewGroup, context, width, height) =
        let viewCount = match box viewGroup with | null -> 0 | _ -> viewGroup.ChildCount
        let bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Rgb565)
        let layout = new Android.Widget.LinearLayout(context, DrawingCacheEnabled = true)
        let canvas, paint = new Canvas(bitmap), new Paint()
        let mutable left, top = 10, 10
        let addElement (element:VisualElement) (child:AndroidView) =
            layout.AddView (child, int element.WidthRequest, int element.HeightRequest)
            child.Layout(left, top, int element.WidthRequest, int element.HeightRequest)
            child.LayoutParameters <- new AndroidViewGroup.LayoutParams (int element.WidthRequest, int element.HeightRequest)
            child.SetBackgroundColor(ViewConversion.ToAndroidColor(element.BackgroundColor))
            top <- top + int element.HeightRequest 
        canvas.DrawBitmap(bitmap, 0.0f, 0.0f, paint)
        for index = 0 to viewCount - 1 do
            let child = viewGroup.GetChildAt(0)
            viewGroup.RemoveView child
            match child with
            | :? ImageRenderer as imageRenderer -> child |> addElement imageRenderer.Element
            | :? LabelRenderer as labelRenderer -> child |> addElement labelRenderer.Element
            | _ -> child |> ignore
        layout.Draw(canvas)
        bitmap
    let mutable formsMap = Unchecked.defaultof<DashboardMap>
    let mutable googleMap = Unchecked.defaultof<GoogleMap>
    let markerViewModel = new Dictionary<string, MarkerViewModel>()
    let subscriptions = new CompositeDisposable()
    let infoWindowClicked _ (eventArgs: GoogleMap.InfoWindowClickEventArgs) = 
        let marker = eventArgs.Marker
        let vm = markerViewModel.[marker.Id]
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
        member this.GetInfoContents(marker: Marker): AndroidView = 
            let vm = markerViewModel.[marker.Id]
            let view = vm |> ViewLocator.Current.ResolveView :?> MarkerView
            view.ViewModel <- vm
            view.Content <- view.CreateContent()
            let infoWidth, infoHeight = view.Content.WidthRequest, view.Content.HeightRequest
            view.WidthRequest <- infoWidth; view.HeightRequest <- infoHeight
            let renderer = Platform.CreateRenderer(view.Content)
            renderer.SetElement(view.Content)
            let vg = renderer.ViewGroup
            vg.LayoutParameters <- new AndroidViewGroup.LayoutParams (int infoWidth, int infoHeight)
            vg.Layout(0, 0, int infoWidth, int infoHeight)
            vg.DrawingCacheEnabled <- true
            renderer.Tracker.UpdateLayout()
            let image = new Android.Widget.ImageView(this.Context)
            image.SetImageBitmap(toBitmap(vg, this.Context, int view.Content.WidthRequest, int view.Content.HeightRequest))
            image :> AndroidView
        member __.GetInfoWindow(marker: Marker): AndroidView = Unchecked.defaultof<AndroidView>
    interface IJavaObject with member __.Handle = base.Handle
    interface IDisposable with member __.Dispose() = base.Dispose()
