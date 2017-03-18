namespace Astrid.Mobile.Common

open XamarinForms.Reactive.FSharp.Themes

open Xamarin.Forms

module Themes =
    let AstridBlue = Color.FromRgb(0, 59, 111)
    let AstridTheme = 
        DefaultTheme 
            |> applyBackgroundColor AstridBlue
            |> applyLabelSetters 
                [
                    new Setter(Property = Label.TextColorProperty, Value = Color.Yellow)
                    new Setter(Property = Label.FontAttributesProperty, Value = FontAttributes.Bold)
                ]

