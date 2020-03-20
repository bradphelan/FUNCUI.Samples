
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

module CompanyView =
    let view (company:Lens<Company>) =

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

    let view (companies: Lens<Company array>)   =

        let updateCompany (company:Company) (companies:Company array)  = 
            updateItems companies company |> Seq.toArray

        DockPanel.create [
            DockPanel.children [
                (* Implement the list box for the bin files *)
                ListBox.create [
                    let companyLenses = 
                        companies.value
                            |> Seq.map ( companies.Focus updateCompany )
                            |> Seq.toArray
                    ListBox.dataItems companyLenses
                    ListBox.itemTemplate (DataTemplateView<Lens<Company>>.create(CompanyView.view))
                ]
            ]
        ]


