
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
open BGS.Data
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
            match run pint32 txt with 
            | Success(result,_,_)->Result.Ok result
            | Failure _-> Result.Error "failed to parse"
        )

module Data =

    type Item = {
        value: int 
    } with
        static member value' = (fun o->o.value),(fun v (o:Item) -> {o with value = v})
        static member init = { value = 0 }

module TextBox =

    open Avalonia.Controls
    open FSharpx

    (* TextBox.onTextChanged is super flaky and causes the dispatch loop to hang because 
       events are fired that are not from user input
    *)
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
            value : string
        }
        with
        static member init = { value = "" }

    let view (stateImage:Image<State*Item>)  = 
        let state = stateImage |> Lens.Tuple.fst
        let item = stateImage |> Lens.Tuple.snd

        let errHandler = Image.ofNone
        TextBox.create [
            TextBox.text (string item.Get.value)
            yield! (item >-> Item.value').Parse errHandler Parsers.int |> TextBox.bindText
        ]

