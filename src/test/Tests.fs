module Tests

open System
open Xunit
open FsUnit

open XTargets.Elmish
open BGS.Data

[<Fact>]
let ``basic lensing`` () =

    let company = BGS.Data.companyFaker.Generate ""
    let image = Redux.ofConst(company)

    let companyName = image >-> Company.name'

    image.Get.name |> should not' (equal "Donkey Gmbh")
    companyName.Set "Donkey Gmbh"
    image.Get.name |> should equal "Donkey Gmbh"


[<Fact>]
let ``lensing through arrays works`` () =
    let company = BGS.Data.companyFaker.Generate ""
    let image = Redux.ofConst(company)
    let employee2 = image >-> Company.employees' >-> (Lens.Array.at 2) >-> Person.firstName'

    image.Get.employees.[2].firstName |> should not' (equal "freddy krueger")
    employee2.Set "freddy krueger"
    image.Get.employees.[2].firstName |> should equal "freddy krueger"



[<Fact>]
let ``Tuple of lenses to a lens of tuples``() =
    let company = BGS.Data.companyFaker.Generate ""

    let ci = Redux.ofConst(company)
    let error = Redux.ofConst(false)

    let ie = Lens.Tuple.mk2 (ci>->Company.revenue') error

    ie.Set(999, true)

    ci.Get.revenue |> should equal 999
    error.Get |> should equal true

    ie.Set(123, false)

    ci.Get.revenue |> should equal 123
    error.Get |> should equal false 

[<Fact>]
let ``Parsing should work``() =
    let company:Company = BGS.Data.companyFaker.Generate ""

    // Set up an image for the company
    let image:Redux<Company> = Redux.ofConst(company)
    // Create an image for for the company revenue by applying a lens
    let revenue:Redux<int> = (image >-> Company.revenue')

    // Setup up an image for the revenue parsing error. If none is passed back then
    // the error message will be "ok" 
    let revenueError:Redux<string option> = Redux.ofConst("").ToOption "ok"

    // Convert the revenue image to a string image and attach an error handling for parsing errors
    let revenueAsString:Redux<string> = revenue.Convert ValueConverters.StringToInt32 revenueError.Set

    revenue.Set(25)
    revenue.Get         |> should equal 25
    revenueAsString.Get |> should equal "25"

    revenueAsString.Set "257"
    revenue.Get         |> should equal 257
    revenueError.Get    |> should equal ""
    revenueAsString.Get |> should equal "257"
    revenueError.Get    |> should equal ""

    revenueAsString.Set "foo"
    revenueError.Get    |> should equal "unable to parse"

    revenueAsString.Set "666"
    revenueAsString.Get |> should equal "666"
    revenueError.Get    |> should equal ""