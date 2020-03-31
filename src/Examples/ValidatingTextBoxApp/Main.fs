
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

    type Item = {
        value0: int 
        value1: int 
    } with
        static member value0' = (fun o->o.value0),(fun v o -> {o with value0 = v})
        static member value1' = (fun o->o.value1),(fun v o -> {o with value1 = v})
        static member init = { value0 = 0; value1 = 1 }


module ItemView =
    open Data

    // The view receives an Image to it's view state tupeled with the model state
    let view (item:Redux<Item>)  = 

        // Get images for each model field
        let value0 = (item >-> Item.value0')
        let value1 = (item >-> Item.value1')

        let inline formField label (value:Redux<int>)  = 
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text label
                        TextBlock.width 150.0
                    ]

                    LocalStateView.create<int,string option> [
                        LocalStateView.state value.Get
                        LocalStateView.viewFunc ( fun (errHandler:Redux<string option>) -> 
                            TextBox.create [
                                TextBox.text (string value.Get)
                                let stringValue = value.Convert ValueConverters.StringToInt32 errHandler.Set
                                yield! stringValue |> TextBox.bindText
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

