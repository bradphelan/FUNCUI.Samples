
namespace BGS

open Avalonia.FuncUI.DSL
open XTargets.Elmish

open Avalonia.Controls
open Avalonia.FuncUI.Components
open Avalonia.Layout
open FSharpx
open BGS.Data
open BGS

module PersonView  =

    let view (isEditable:bool) (person:Redux<Person>) =
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

    type State = {
        revenueErrors:obj array
    }
    with
        static member revenueErrors' = (fun o->o.revenueErrors),(fun v o -> {o with revenueErrors = v})
    
    let init = { 
        revenueErrors = [||]
    }

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

    let view   (editable:bool) (company:Redux<Company>) (viewState:Redux<State>) =
        let errHandler = fun (v:string option) -> 
            v
            |> Option.toArray
            |> Seq.cast<obj>
            |> Seq.toArray
            |> (viewState >-> State.revenueErrors').Set 

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
                            yield! (company >-> Company.revenue').Convert XTargets.Elmish.ValueConverters.StringToInt32 errHandler |> TextBox.bindText
                            TextBox.errors viewState.Get.revenueErrors
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

    let view ( company:Redux<Company>)  =

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
                // TODO
                let companyViewState = Redux.ofValue CompanyView.init
                CompanyView.view true company companyViewState 
                TextBlock.create [ 
                    TextBlock.text "The employees ( you can edit them )" 
                    TextBlock.fontSize 20.0
                    TextBlock.margin 10.0
                ]
                ListBox.create [
                    ListBox.dataItems personLenses
                    ListBox.itemTemplate (DataTemplateView<Redux<Person>>.create(PersonView.view true))
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

    let view (state: Redux<State>)   =

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

                    // TODO
                    let companyViewState = Redux.ofValue CompanyView.init
                    ListBox.itemTemplate (DataTemplateView.create( fun item -> CompanyView.view false item companyViewState  ))
                ]

                let selectedCompanyLensId = 
                    state 
                    >-> State.companies' 
                    >-> (Lens.Array.find (fun c -> c.id = state.Get.selectedCompany))

                CompanyDetailsView.view selectedCompanyLensId
            ]
        ]


