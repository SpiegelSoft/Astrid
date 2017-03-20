namespace FSharpResourcesDemo

open System

open Xamarin.Forms.Platform.Android

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget

type MainPage() =
    inherit Xamarin.Forms.ContentPage()
    let layout = new Xamarin.Forms.StackLayout()
    do layout.Children.Add(new Xamarin.Forms.Button(Image = new Xamarin.Forms.FileImageSource(File = "Icon.jpg")))
    do base.Content <- layout

type App() =
    inherit Xamarin.Forms.Application()
    do base.MainPage <- new MainPage()

[<Activity (Label = "FSharpResourcesDemo", MainLauncher = true)>]
type MainActivity () =
    inherit FormsApplicationActivity()
    override this.OnCreate (bundle) =
        base.OnCreate (bundle)
        Xamarin.Forms.Forms.Init(this, bundle)
        this.LoadApplication(new App())



