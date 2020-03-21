
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
    let updatePersonFirstName (person:Lens<Person>) name =
        {person.Get with firstName = name} |> person.Set

    let updatePersonSecondName (person:Lens<Person>) name =
        {person.Get with lastName = name} |> person.Set

    let updateCompanyBusiness (company:Lens<Company>) business =
        {company.Get with business = business} |> company.Set

    let view (isEditable:bool) (person:Lens<Person>) =
        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.isEnabled isEditable
                    TextBox.text person.Get.firstName
                    if isEditable then
                        yield! TextBox.onTextInput ( fun txt ->  updatePersonFirstName person txt)
                ]
                TextBox.create [
                    TextBox.isEnabled isEditable
                    TextBox.width 200.0
                    TextBox.text person.Get.lastName
                    if isEditable then
                        yield! TextBox.onTextInput ( fun txt ->  updatePersonSecondName person txt)
                ]
            ]
        ]


module CompanyView =
    let updateCompanyName (company:Lens<Company>) name =
        if name <> company.Get.name then
            {company.Get with name = name} |> company.Set

    let updateCompanyBusiness (company:Lens<Company>) business =
        if business <> company.Get.business then
            {company.Get with business = business} |> company.Set

    let view   (editable:bool) (company:Lens<Company>) =
        DockPanel.create [
            DockPanel.children [
                StackPanel.create [
                    StackPanel.orientation Orientation.Horizontal
                    StackPanel.children [
                        TextBox.create [
                            TextBox.isEnabled editable
                            TextBox.width 200.0
                            TextBox.text company.Get.name
                            yield! TextBox.onTextInput ( fun txt ->  updateCompanyName company txt )
                        ]
                        TextBox.create [
                            TextBox.isEnabled editable
                            TextBox.width 200.0
                            TextBox.text company.Get.business
                            yield! TextBox.onTextInput ( fun txt ->  updateCompanyBusiness company txt )
                        ]
                    ]
                ]
            ]
        ]
       
module CompanyDetailsView =
    let updatePersons (person:Person) (persons:Person array)  =
        updateItems persons person |> Seq.toArray

    let view ( company:Lens<Company>)  =

        let persons =
            let setter persons state = { state with employees = persons }
            let getter state = state.employees 
            company.Focus setter getter

        let personLenses = 
            Lens.focusArray (fun (a:Person) (b:Person) -> a.id = b.id) persons


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
    }

    let init companies = { companies = companies; selectedCompany = 0}

    let view (state: Lens<State>)   =

        let updateSelectedCompany selected (state:State) =
            {state with selectedCompany = selected}

        let findSelectedIndex (state:State) =
            state.companies |> Seq.findIndex ( fun c -> c.id = state.selectedCompany)

        DockPanel.create [
            DockPanel.children [
                let companyLenses = 
                    state.Focus (fun v s -> {s with companies = v}) (fun v -> v.companies)
                    |> Lens.focusArray (fun a b -> a.id = b.id)

                let selectedCompanyIdLens =
                    state.Focus updateSelectedCompany (fun x -> x.selectedCompany ) 

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
        
                    state.Focus setter getter

CompanyDetailsView.view selectedCompanyLens 
            ]
        ]


