
namespace BGS

open Avalonia.FuncUI.DSL
open XTargets.Elmish
open System
open Avalonia.Controls
open Avalonia.Layout
open FSharpx
open Avalonia

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
    // This is the view state
    type State =
        {
            value0ParseError: string option
            value1ParseError: string option
        }
        with
        static member value0ParseError' = (fun o->o.value0ParseError),(fun v o -> {o with value0ParseError = v})
        static member value1ParseError' = (fun o->o.value1ParseError),(fun v o -> {o with value1ParseError = v})
        static member init = { value0ParseError = None; value1ParseError=None }

    // The view receives an Image to it's view state tupeled with the model state
    let view (stateImage:Image<State*Item>)  = 

        // Split the data into seperate images
        let state = stateImage |> Lens.Tuple.fst
        let item = stateImage |> Lens.Tuple.snd

        // Get images for each field
        let value0ParseErrors = state >-> State.value0ParseError' 
        let value1ParseErrors = state >-> State.value1ParseError' 

        // Get images for each model field
        let value0 = (item >-> Item.value0')
        let value1 = (item >-> Item.value1')

        let value0' = Lens.Tuple.mk2 value0 value0ParseErrors
        let value1' = Lens.Tuple.mk2 value1 value1ParseErrors

        let validate v = 
            if v > 10 then
                [|"the value must be less than 10"|]
            else
                [||]

        let inline formField label (value:Image<int>) (errHandler:Image<string option>) = 
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text label
                        TextBlock.width 150.0
                    ]
                    TextBox.create [
                        TextBox.text (string value.Get)
                        yield! 
                            value.Parse errHandler.Set ValueConverters.StringToInt32 
                            |> TextBox.bindText
                        let validationErrors = validate value.Get 
                        let parseErrors = 
                            errHandler.Get 
                            |> Option.toArray
                        let combinedErrors = 
                            [parseErrors; validationErrors] 
                            |> Seq.concat 
                            |> Seq.cast<obj> 
                        TextBox.errors combinedErrors
                        TextBox.width 150.0
                    ]
                ]
            ]

        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                formField "Value 0" value0 value0ParseErrors
                formField "Value 1" value1 value0ParseErrors
            ]
        ]

