
namespace BGS

open Avalonia.FuncUI.DSL
open XTargets.Elmish

open System
open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open System.IO
open FSharpx
open Elmish
open BGS.Data
open BGS

module PersonView  =

    let view (isEditable:bool) (person:Image<Person>) =
        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.isEnabled isEditable
                    TextBox.text person.Get.firstName
                    if isEditable then
                        yield! TextBox.onTextInput (person >-> Person.firstName').Set
                ]
                TextBox.create [
                    TextBox.isEnabled isEditable
                    TextBox.width 200.0
                    TextBox.text person.Get.lastName
                    if isEditable then
                        yield! TextBox.onTextInput (person >-> Person.lastName').Set
                ]
            ]
        ]

module CompanyView =

    module Parsers = 
        // Usa FParsec to parse a string to an int. Overkill but
        // a nice demonstration. You could make it more complex
        open FParsec
        let int = (
            ( fun (v:int) -> sprintf "%d" v), 
              fun (txt:string) -> 
                match run pint32 txt with 
                | Success(result,_,_)->Some result
                | Failure _->None
            )

        open FParsec
        let intAsync = (
            ( fun (v:int32) -> sprintf "%d" v), 
              fun (txt:string) -> 
                async {
                    do! Async.Sleep 1000
                    match run pint32 txt with 
                    | Success(result,_,_)->return Some result
                    | Failure _->return None
                } 
                |> Async.StartAsTask
                |> Async.AwaitTask
            )


    module TextBox =
        let inline bindText (lens:Image<string>) =
            [
                TextBox.text lens.Get
                yield! TextBox.onTextInput lens.Set
            ]

    let view   (editable:bool) (company:Image<Company>) =

        DockPanel.create [
            DockPanel.children [
                StackPanel.create [
                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.children [
                        TextBox.create [
                            TextBox.isEnabled editable
                            TextBox.width 200.0
                            yield! company >-> Company.name' |> TextBox.bindText
                        ]
                        TextBox.create [
                            TextBox.isEnabled editable
                            TextBox.width 200.0
                            yield! company >-> Company.business' |> TextBox.bindText
                        ]
                        TextBox.create [
                            TextBox.isEnabled editable
                            TextBox.width 200.0
                            yield! company >-> Company.revenue' >-> Parsers.intAsync |> TextBox.bindText
                        ]
                        StackPanel.create [
                            StackPanel.orientation Orientation.Vertical
                            StackPanel.children [
                                let revenue = company >-> Company.revenue'
                                let update n =
                                        revenue.Update(fun v -> 
                                            async {
                                                do! Async.SwitchToThreadPool()
                                                do! Async.Sleep 50
                                                return v+n
                                            }) 
                                Button.create [
                                    Button.content "+ async"
                                    Button.onClick (fun _ -> update 1  )
                                ]
                                Button.create [
                                    Button.content "- async"
                                    Button.onClick (fun _ -> update -1  )
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
       
module CompanyDetailsView =

    let updatePersons (person:Person) (persons:Person array)  =
        updateItems persons person |> Seq.toArray

    let view ( company:Image<Company>)  =

        let personLenses = 
            Lens.Array.each (company >-> Company.employees')

        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                TextBlock.create [ 
                    TextBlock.text "The Company ( you can edit me )" 
                    TextBlock.fontSize 20.0
                    TextBlock.margin 10.0
                ]
                CompanyView.view true company 
                TextBlock.create [ 
                    TextBlock.text "The employees ( you can edit them )" 
                    TextBlock.fontSize 20.0
                    TextBlock.margin 10.0
                ]
                ListBox.create [
                    ListBox.dataItems personLenses
                    ListBox.itemTemplate (DataTemplateView<Image<Person>>.create(PersonView.view true))
                ]
            ]
        ]

module CompaniesView =

    type State = {
        companies: Company array
        selectedCompany: int
    } with
        static member companies' = (fun o->o.companies),(fun v o -> {o with companies = v})
        static member selectedCompany' = (fun o->o.selectedCompany),(fun v o -> {o with selectedCompany = v})

    let init companies = { companies = companies; selectedCompany = 0}

    let view (state: Image<State>)   =

        let findSelectedIndex (state:State) =
            state.companies |> Seq.findIndex ( fun c -> c.id = state.selectedCompany)

        DockPanel.create [
            DockPanel.children [
                let companyLenses = 
                    state >-> State.companies' |> Lens.Array.each 

                TextBlock.create [
                    TextBlock.dock Dock.Bottom
                    TextBlock.text (sprintf "%d" state.Get.selectedCompany)
                ]
                ListBox.create [
                    ListBox.dock Dock.Left
                    ListBox.dataItems companyLenses
                    ListBox.selectedIndex (findSelectedIndex(state.Get))
                    ListBox.onSelectedIndexChanged ( fun id -> 
                        if id > -1 then state.Get.companies.[id].id |> (state >-> State.selectedCompany').Set 
                    )
                    ListBox.itemTemplate (DataTemplateView.create(CompanyView.view false  ))
                ]

                let selectedCompanyLensId = 
                    state 
                    >-> State.companies' 
                    >-> (Lens.Array.find (fun c -> c.id = state.Get.selectedCompany))

                CompanyDetailsView.view selectedCompanyLensId
            ]
        ]


