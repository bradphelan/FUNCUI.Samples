
namespace BGS

open Microsoft.FSharp.Reflection
open System.Globalization
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Elmish

// Extension methods
open System.Runtime.CompilerServices

/// Helpers for working with reflection on discriminated unions
module DU =
    let toString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

    let fromString<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        |_ -> None

    let fromStringOrFail<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> FSharpValue.MakeUnion(case,[||]) :?> 'a
        |_ -> failwithf  "Unable to convert %s to DU of type %s" s (typeof<'a>.ToString())

module Option =
    let failIfNone msg o =
        match o with
        | Some v -> v
        | None -> failwith msg 

module Text =

    // Neat method of finding the TryParse method for any type that supports it.
    // See https://stackoverflow.com/a/33161245/158285
    let inline tryParseWithDefault defaultVal text : ^a option = 
      let mutable r = defaultVal
      if (^a : (static member TryParse: string * ^a byref -> bool) (text, &r)) 
      then Some r
      else None

    let inline tryParse text = tryParseWithDefault (Unchecked.defaultof<_>) text

module TextBox =

    open Avalonia.Controls
    open FSharpx

    /// Parses the text into the msg. Uses type inference to figure out the type of parser
    /// required
    let inline onTextChangedParsed msg dispatch =
        TextBox.onTextChanged ( Text.tryParse >> Option.map msg >> Option.map dispatch >> ignore )

module Cmd =
    open Elmish
    let choose (f: 'a -> 'msg option) (cmd: Cmd<'a>) : Cmd<'msg> =
        let foo dispatch x =
            match f(x) with
            | Some y -> dispatch(y)
            | None -> ()
        cmd |> List.map (fun g -> (fun dispatch -> ( foo dispatch )) >> g)

type TempFile() =
    let path = System.IO.Path.GetTempFileName()
    member x.Path = path
    interface System.IDisposable with
        member x.Dispose() = System.IO.File.Delete(path)

module Layout =
    open Avalonia.Controls

    /// Wrap a panel with a tile and border 
    let borderfy title panel =
        Border.create [
            Border.borderThickness 1.0
            Border.borderBrush "lightgray"
            Border.padding 5.0
            Border.child (
                DockPanel.create [
                    DockPanel.children [
                        TextBlock.create [
                            TextBlock.text title
                            TextBlock.dock Dock.Top
                            TextBlock.margin 2.5
                        ]
                        panel
                    ]
                ]
            )
        ]
        :> IView

    /// Wrap the panel in a dock panel and put it at the top 
    let topify panel = 
        DockPanel.create [
            DockPanel.children [
                WrapPanel.create [
                    WrapPanel.dock Dock.Top
                    WrapPanel.children [ panel ]
                ]
            ]
        ]
        :> IView

module Control =
    open Avalonia.Styling
    open Avalonia.Controls
    open FSharpx
    let styling stylesList = 
        let styles = Styles()
        for style in stylesList do
            styles.Add style
        Control.styles styles

    let foo = 10

    let style (selector:System.Func<Selector,Selector>) (setters:IAttr<'a> seq) =
        let s = Style(selector  )
        for attr in setters do
            match attr.Property with
            | Some p -> 
                match p.accessor with
                | InstanceProperty x -> failwith "Can't support instance property" 
                | AvaloniaProperty x -> s.Setters.Add(Setter(x,p.value))
            | None -> ()
        s

