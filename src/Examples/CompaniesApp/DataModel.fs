namespace BGS
open Bogus


module Data =

    type Person = {
        id: int 
        firstName: string
        lastName: string
    }
    let personFaker = 
        Bogus
            .Faker<Person>()
            .CustomInstantiator(fun f ->
            { 
                id = f.IndexGlobal
                firstName = f.Name.FirstName()
                lastName = f.Name.LastName()
            }
        )


    type Company = {
        id: int
        name: string
        business: string
        employees: Person array
    }
    let companyFaker = 
        Bogus
            .Faker<Company>()
            .CustomInstantiator( fun f ->
                { 
                    id = f.IndexGlobal
                    name = f.Company.CompanyName()
                    business = f.Commerce.ProductName()
                    employees = personFaker.GenerateForever()
                        |> Seq.take(3) 
                        |> Seq.toArray
                    })


    type Directory = Company array

    let init = companyFaker.GenerateForever() |> Seq.take 5 |> Seq.toArray

    /// Check if the ids are the same
    let inline getId (x:^a) = (^a : (member id : int)(x))

    let inline replaceIfSame (newItem:'a) (oldItem:'a) = 
        if getId(oldItem) = getId(newItem) then newItem else oldItem 
    let inline updateItems (items:'a seq) (item:'a) = 
        items |> Seq.map ( replaceIfSame item )

