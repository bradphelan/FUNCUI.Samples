
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
    let view (isEditable:bool) (person:Lens<Person>) =
        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.isEnabled isEditable
                    TextBox.text person.Get.firstName
                    if isEditable then
                        yield! TextBox.onTextInput (person.Focus Person.firstName').Set
                ]
                TextBox.create [
                    TextBox.isEnabled isEditable
                    TextBox.width 200.0
                    TextBox.text person.Get.lastName
                    if isEditable then
                        yield! TextBox.onTextInput (person.Focus Person.lastName').Set
                ]
            ]
        ]

module CompanyView =
    open Lens.Operators

    module Morphs = 
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

    module TextBox =
        let inline bindText (lens:Lens<string>) =
            [
                TextBox.text lens.Get
                yield! TextBox.onTextInput lens.Set
            ]

    let view   (editable:bool) (company:Lens<Company>) =

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
                            yield! company >-> Company.revenue' >?> Morphs.int |> TextBox.bindText
                        ]
                    ]
                ]
            ]
        ]
       
module CompanyDetailsView =
    let updatePersons (person:Person) (persons:Person array)  =
        updateItems persons person |> Seq.toArray

    let view ( company:Lens<Company>)  =

        let personLenses = 
            Lens.focusArray (fun (a:Person) (b:Person) -> a.id = b.id) (company.Focus Company.employees')

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
                    ListBox.itemTemplate (DataTemplateView<Lens<Person>>.create(PersonView.view true))
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

    let view (state: Lens<State>)   =

        let updateSelectedCompany selected (state:State) =
            {state with selectedCompany = selected}

        let findSelectedIndex (state:State) =
            state.companies |> Seq.findIndex ( fun c -> c.id = state.selectedCompany)

        DockPanel.create [
            DockPanel.children [
                let companyLenses = 
                    state.Focus State.companies' |> Lens.focusArray (fun a b -> a.id = b.id)

                let selectedCompanyIdLens =
                    state.Focus State.selectedCompany'

                TextBlock.create [
                    TextBlock.dock Dock.Bottom
                    TextBlock.text (sprintf "%d" state.Get.selectedCompany)
                ]
                ListBox.create [
                    ListBox.dock Dock.Left
                    ListBox.dataItems companyLenses
                    ListBox.selectedIndex (findSelectedIndex(state.Get))
                    ListBox.onSelectedIndexChanged ( fun id -> 
                        if id > -1 then state.Get.companies.[id].id |> selectedCompanyIdLens.Set 
                    )
                    ListBox.itemTemplate (DataTemplateView.create(CompanyView.view false  ))
                ]

                (* Ideally we would have a structure like so

                    let selectedCompanyLens = state
                        .Focus(<@ fun x -> x.companies @>)
                        .FocusArrayItem(state.Get.selectedCompany)

                    But it requires some more infrustructure that 
                    I haven't time to put in yet
                *)
                let selectedCompanyLens = 
                    let setter (c:Company) (s:State) = 
                        { s with 
                            companies = 
                            s.companies 
                            |> Seq.map ( fun c' -> if c'.id = s.selectedCompany then c else c') 
                            |> Seq.toArray
                        }
                    let getter (s:State) =
                        s.companies 
                        |> Seq.find ( fun c -> c.id = s.selectedCompany)
        
                    state.Focus(getter,setter)

CompanyDetailsView.view selectedCompanyLens 
            ]
        ]


