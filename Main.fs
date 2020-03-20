
namespace BGS

open Avalonia.FuncUI.DSL
open BGS.Elmish

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
        {person.value with firstName = name} |> person.Set

    let updatePersonSecondName (person:Lens<Person>) name =
        {person.value with firstName = name} |> person.Set

    let updateCompanyBusiness (company:Lens<Company>) business =
        {company.value with business = business} |> company.Set

    let view (person:Lens<Person>) =
        StackPanel.create [
            StackPanel.orientation Orientation.Vertical
            StackPanel.children [
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.text person.value.firstName
                    TextBox.onTextChanged <| updatePersonFirstName person
                ]
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.text person.value.lastName
                    TextBox.onTextChanged <| updatePersonSecondName person
                ]
            ]
        ]


module EmployeesView =
    let updatePersons (person:Person) (persons:Person array)  =
        updateItems persons person |> Seq.toArray

    let view (persons:Lens<Person array>) =
        ListBox.create [
            let personLenses  = 
                persons.value
                    |> Seq.map ( persons.Focus updatePersons )
                    |> Seq.toArray

            ListBox.dataItems personLenses
            ListBox.itemTemplate (DataTemplateView<Lens<Person>>.create(PersonView.view))
        ]

module CompanyView =
    let view (selectedCompanyLens:Lens<int>)  (company:Lens<Company>) =

        let updateCompanyName (company:Lens<Company>) name =
            {company.value with name = name} |> company.Set

        let updateCompanyBusiness (company:Lens<Company>) business =
            {company.value with business = business} |> company.Set

        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.text company.value.name
                    TextBox.onTextChanged <| updateCompanyName company
                ]
                TextBox.create [
                    TextBox.width 200.0
                    TextBox.text company.value.business
                    TextBox.onTextChanged <| updateCompanyBusiness company
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

        let updateCompany company (state:State) = 
            {state with companies = updateItems state.companies company |> Seq.toArray }

        let updateSelectedCompany selected (state:State) =
            {state with selectedCompany = selected}

        DockPanel.create [
            DockPanel.children [
                ListBox.create [
                    let companyLenses = 
                        state.value.companies
                            |> Seq.map ( state.Focus updateCompany )
                            |> Seq.toArray
                    let selectedCompanyLens =
                        state.Focus updateSelectedCompany 0

                    ListBox.dock Dock.Left
                    ListBox.dataItems companyLenses
                    ListBox.itemTemplate (DataTemplateView<Lens<Company>>.create(CompanyView.view selectedCompanyLens))
                ]
                let selectedCompanyLens =
                    state.value.companies
                    |> Seq.find( fun company -> company.id = state.value.selectedCompany)
                    |> state.Focus updateCompany



                let employeesLens =
                    selectedCompanyLens.Focus 
                        (fun (employees:Person array) (company:Company) -> { company with employees = employees})
                        selectedCompanyLens.value.employees

                EmployeesView.view employeesLens
            ]
        ]


