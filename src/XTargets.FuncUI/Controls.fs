namespace XTargets.Elmish

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open XTargets.Elmish

module TextBox =
    let onTextInput handler =
        [
            TextBox.onKeyDown ( fun args ->  handler (args.Source :?> TextBox).Text  )
            TextBox.onKeyUp ( fun args ->  handler (args.Source :?> TextBox).Text  )
        ]

    let inline bindText (redux:Redux<string>) =
        [
            TextBox.text redux.Get
            yield! onTextInput redux.Set
        ]

