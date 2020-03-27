
namespace BGS

open Avalonia.FuncUI.DSL
open XTargets.Elmish

open System
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Components.Hosts
open Avalonia.Layout
open System.IO
open FSharpx
open Elmish
open BGS
open Elmish
open XTargets.Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open FSharpx
open System.IO

module Parsers = 
    // Usa FParsec to parse a string to an int. Overkill but
    // a nice demonstration. You could make it more complex
    open FParsec
    let int = (
        ( fun (v:int) -> sprintf "%d" v), 
          fun (txt:string) -> 
            let r,v = Int32.TryParse txt
            if r then
                Result.Ok v
            else
                Result.Error (sprintf "failed to parse %s as Int32" txt)
        )

module Data =

    type Item = {
        value0: int 
        value1: int 
    } with
        static member value0' = (fun o->o.value0),(fun v o -> {o with value0 = v})
        static member value1' = (fun o->o.value1),(fun v o -> {o with value1 = v})

        static member init = { value0 = 0; value1 = 1 }

module TextBox =
    let onTextInput handler =
        [
            TextBox.onKeyDown ( fun args ->  handler (args.Source :?> TextBox).Text  )
            TextBox.onKeyUp ( fun args ->  handler (args.Source :?> TextBox).Text  )
        ]

    let inline bindText (lens:Image<string>) =
        [
            TextBox.text lens.Get
            yield! onTextInput lens.Set
        ]

module ItemView =
    open Data
    type State =
        {
            value0Errors: string array
            value1Errors: string array
        }
        with
        static member value0Errors' = (fun o->o.value0Errors),(fun v o -> {o with value0Errors = v})
        static member value1Errors' = (fun o->o.value1Errors),(fun v o -> {o with value1Errors = v})
        static member init = { value0Errors = [||]; value1Errors=[||] }

    let view (stateImage:Image<State*Item>)  = 
        let state = stateImage |> Lens.Tuple.fst
        let item = stateImage |> Lens.Tuple.snd

        let value0Errors = state >-> State.value0Errors' >-> Lens.Array.toOption
        let value1Errors = state >-> State.value1Errors' >-> Lens.Array.toOption

        let value0 = (item >-> Item.value0')
        let value1 = (item >-> Item.value1')

        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                StackPanel.create [
                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.children [
                        TextBlock.create [
                            TextBlock.text "Value0"
                            TextBlock.width 150.0
                        ]
                        TextBox.create [
                            TextBox.text (string item.Get.value0)
                            yield! value0.Parse value0Errors Parsers.int |> TextBox.bindText
                            TextBox.errors (state.Get.value0Errors |> Seq.cast<obj> |> Seq.toArray)
                            TextBox.width 150.0
                        ]
                    ]
                ]
                StackPanel.create [
                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.children [
                        TextBlock.create [
                            TextBlock.text "Value1"
                            TextBlock.width 150.0
                        ]
                        TextBox.create [
                            TextBox.text (string item.Get.value1)
                            yield! value1.Parse value1Errors Parsers.int |> TextBox.bindText
                            TextBox.errors (state.Get.value1Errors |> Seq.cast<obj> |> Seq.toArray)
                            TextBox.width 150.0
                        ]
                    ]
                ]

            ]
        ]

