module Tests

open System
open Xunit
open FsUnit

open XTargets.Elmish
open BGS.Data

// Create a simple image of the data
// that can be updated with lenses
let simpleImage(data')=
    let mutable data = data'
    let dispatch update =
        data <- update data
    let getter = fun()->data
    Image(getter, dispatch)

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

[<Fact>]
let ``basic lensing`` () =

    let company = BGS.Data.companyFaker.Generate ""
    let image = simpleImage(company)

    let companyName = image >-> Company.name'

    image.Get.name |> should not' (equal "Donkey Gmbh")
    companyName.Set "Donkey Gmbh"
    image.Get.name |> should equal "Donkey Gmbh"


[<Fact>]
let ``lensing through arrays works`` =
    let company = BGS.Data.companyFaker.Generate ""
    let image = simpleImage(company)
    let employee2 = image >-> Company.employees' >-> (Lens.Array.at 2) >-> Person.firstName'

    image.Get.employees.[2].firstName |> should not' (equal "freddy krueger")
    employee2.Set "freedy krueger"
    image.Get.employees.[2].firstName |> should equal "freddy krueger"


[<Fact>]
let ``Parsing should work`` =
    let company = BGS.Data.companyFaker.Generate ""
    let image = simpleImage(company)
    let revenueAsString = image >-> Company.revenue' >-> Parsers.int
    let revenue = image >-> Company.revenue' 
    revenue.Set(25)
    revenue.Get |> should equal 25
    revenueAsString |> should equal "25"
    revenueAsString.Set "257"
    revenue.Get |> should equal 257
    revenueAsString |> should equal "257"
