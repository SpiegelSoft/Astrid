namespace Astrid.Mobile.Common

open XamarinForms.Reactive.FSharp

type Configuration() =
    let [<Literal>] AppName = "Astrid";
    interface IConfiguration with
        member __.MobileServiceUri = None
        member __.AppName = AppName
