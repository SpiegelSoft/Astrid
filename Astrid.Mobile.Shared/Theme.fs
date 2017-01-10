namespace Astrid.Mobile.Shared

open XamarinForms.Reactive.FSharp.Themes

open Xamarin.Forms

module Themes =
    let AstridTheme = 
        DefaultTheme 
            |> withBackgroundColor (Color.Navy)
            |> withLabelSetters 
                [
                    new Setter(Property = Label.TextColorProperty, Value = Color.Yellow)
                    new Setter(Property = Label.FontAttributesProperty, Value = FontAttributes.Bold)
                ]

