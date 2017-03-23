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
    let googleMarker = new Dictionary<MarkedLocation, Marker>()
    let pinAdded (markedLocation: MarkedLocation) =
        let markerOptions = new MarkerOptions()
        match markedLocation.ViewModel.Details with
        | GeocodingResult _ -> markerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed)) |> ignore
        | PlaceOfInterest _ -> markerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueViolet)) |> ignore
        markerOptions.SetPosition(new LatLng(markedLocation.Location.Latitude / 1.0<deg>, markedLocation.Location.Longitude / 1.0<deg>)) |> ignore
        let marker = googleMap.AddMarker markerOptions
        markerViewModel.[marker.Id] <- markedLocation.ViewModel
        googleMarker.[markedLocation] <- marker
    let pinRemoved (markedLocation: MarkedLocation) =
        let marker = googleMarker.[markedLocation]
        marker.Remove()
        marker.Dispose()
    let subscriptions = new CompositeDisposable()
    let infoWindowClicked _ (eventArgs: GoogleMap.InfoWindowClickEventArgs) = 
        let marker = eventArgs.Marker
        let vm = markerViewModel.[marker.Id]
        let x = Xamarin.Forms.Platform.Android.ResourceManager.DrawableClass
        let y = Xamarin.Forms.Platform.Android.ResourceManager.ResourceClass
        match vm.Details with
        | GeocodingResult result -> vm.Screen.Router.Navigate.Execute(new GeocodingResultViewModel(vm.Location, vm.PlaceOfInterest, vm.ConvertToPlaceOfInterestCommand, vm.Screen)) |> ObservableExtensions.ignoreOnce
        | PlaceOfInterest placeOfInterest -> vm.Screen.Router.Navigate.Execute(new TimelineViewModel(placeOfInterest, vm.DeletePlaceOfInterestCommand)) |> ObservableExtensions.ignoreOnce
    let infoWindowEventHandler = new EventHandler<GoogleMap.InfoWindowClickEventArgs>(infoWindowClicked)
    override this.OnElementChanged e =
        base.OnElementChanged(e)
        match box e.OldElement with
        | null -> e |> ignore
        | _ -> googleMap.InfoWindowClick.RemoveHandler infoWindowEventHandler
        match box e.NewElement with
        | null -> e |> ignore
        | _ -> formsMap <- e.NewElement
    override __.Dispose(disposing) = 
        if disposing then 
            formsMap.PinnedLocations |> Seq.iter pinRemoved
            markerViewModel.Clear()
            formsMap.Close()
            subscriptions.Clear()
    override this.OnDraw(canvas) =
        base.OnDraw(canvas)
    interface IOnMapReadyCallback with 
        override this.OnMapReady map =
            base.OnMapReady map
            googleMap <- map
            formsMap.PinnedLocations.ItemsAdded.ObserveOn(RxApp.MainThreadScheduler).Subscribe(pinAdded) |> subscriptions.Add
            formsMap.PinnedLocations.ItemsRemoved.ObserveOn(RxApp.MainThreadScheduler).Subscribe(pinRemoved) |> subscriptions.Add
            googleMap.InfoWindowClick.AddHandler infoWindowEventHandler
            googleMap.SetInfoWindowAdapter this
            googleMap.Clear()
            markerViewModel.Clear()
            formsMap.PinnedLocations |> Seq.iter pinAdded
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
