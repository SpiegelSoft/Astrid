namespace Astrid.Mobile.Droid

open Xamarin.Forms.Platform.Android
open Xamarin.Forms

open XamarinForms.Reactive.FSharp

open Android.Gms.Maps.Model

type SearchResultEffect() =
    inherit PlatformEffect()
    member val private Map = Option<GeographicMap>.None with get, set
    override this.OnAttached() = this.Map <- this.Element :?> GeographicMap |> Some
    override this.OnDetached() = this.Map <- None
    override this.OnElementPropertyChanged(e) = 
        match e.PropertyName with
        | "VisibleRegion" ->
            let marker = new MarkerOptions()
            let options = marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueCyan))
            options |> ignore
        | _ -> e |> ignore

[<assembly: ResolutionGroupName("Astrid")>]
[<assembly: ExportEffect(typeof<SearchResultEffect>, "SearchResultEffect")>]
()
