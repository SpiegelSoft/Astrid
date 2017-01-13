namespace Astrid.Mobile.Shared

open Xamarin.Forms.Maps

open XamarinForms.Reactive.FSharp

type Configuration() =
    let [<Literal>] AppName = "Astrid";
    interface IConfiguration with
        member __.MobileServiceUri = None
        member __.AppName = AppName

type IAstridPlatform = 
    inherit IPlatform
    abstract member Geocoder: Geocoder
