namespace BGS

open Avalonia.FuncUI.DSL
open XTargets.Elmish
open System
open Avalonia.Controls
open Avalonia.Layout
open FSharpx
open Avalonia
open Avalonia.FuncUI.Types

module Data =

    // Create a simple model with two int fields
    type Item = {
        value0: int 
        value1: int 
    } with
        // Provide lenses for focusing on seperate fields
        static member value0' = (fun o->o.value0),(fun v o -> {o with value0 = v})
        static member value1' = (fun o->o.value1),(fun v o -> {o with value1 = v})

        // Provide an initializer
        static member init = { value0 = 0; value1 = 1 }


module ItemView =

    // the view recieves a `Redux` or a pointer to the data it needs to 
    // render and update. The `Redux` object has Get and Set methods.
    // Calling `Set` fires the dispatcher with a message that knows 
    // how to do the update on the root data. 
    let view (item:Redux<Data.Item>) = 

        // Get a redux for each sub property by using the lens combinators
        let value0:Redux<int> = (item >-> Data.Item.value0')
        let value1:Redux<int> = (item >-> Data.Item.value1')

        // Generate a form field for a specific property
        let inline formField label (value:Redux<int>)  = 
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [

                    TextBlock.create [
                        TextBlock.text label
                        TextBlock.width 150.0
                    ]

                    LocalStateView.create [

                        // Set state so that the patch algorithm knows to rerender if the value changes
                        LocalStateView.state value.Get

                        // Set the view function for rendering. The view function should
                        // take 1 parameter being a Redux<'a> when 'a : equality. In this
                        // case we want the errHandler to be our local state and we
                        // want `string option` though it could be almost anything we want
                        LocalStateView.viewFunc ( fun (errHandler:Redux<string option>) -> 
                            TextBox.create [
                                // Render the current value for the text
                                TextBox.text (string value.Get)
                                // Convert the Redux<int> to Redux<string> via a two way value converter.
                                // The setter of a Redux<string option> is passed to collect any parsing 
                                // errors. Notice that `errHandler` is the local state that is passed
                                // into the view. It doesn't not propagate out of this view. It will
                                // always be reset to the default value if the state propery is updated
                                let stringValue:Redux<string> = value.Convert ValueConverters.StringToInt32 errHandler.Set

                                // Bind the Set and Get methods of the stringValue to the TextBox. See bindText
                                yield! stringValue |> TextBox.bindText

                                // Collect the current parse errors and store them in the errors field
                                // of the textbox
                                let parseErrors = 
                                    errHandler.Get 
                                    |> Option.toArray
                                    |> Seq.cast<obj> 
                                TextBox.errors parseErrors

                                TextBox.width 150.0
                            ] :> IView
                            
                        )
                    ]

                ]
            ]

        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                formField "Value 0" value0 
                formField "Value 1" value1 
            ]
        ]

