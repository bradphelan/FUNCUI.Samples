
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
        let valueParse0Errors = state >-> State.value0ParseError' 
        let value1ParseErrors = state >-> State.value1ParseError' 

        // Get images for each model field
        let value0 = (item >-> Item.value0')
        let value1 = (item >-> Item.value1')

        let validate v = 
            if v > 10 then
                [|"the value must be less than 10"|]
            else
                [||]

        let inline bindValidation (data:Image<'a>) parser (errHandler:Image<string option>) (validate:'a->string array) =
            [
                TextBox.text (string data.Get)
                yield! data.Parse errHandler parser |> TextBox.bindText
                TextBox.errors ([errHandler.Get |> Option.toArray; validate data.Get] |> Seq.concat |> Seq.cast<obj> |> Seq.toArray)

            ]


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
                            yield! bindValidation value0 Parsers.int valueParse0Errors validate
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
                            yield! bindValidation value1 Parsers.int value1ParseErrors validate
                            TextBox.width 150.0
                        ]
                    ]
                ]

            ]
        ]

