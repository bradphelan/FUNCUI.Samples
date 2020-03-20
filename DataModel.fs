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

    type Location = {
        id: int
        address: string
        people: Person array
    }
    let locationFaker = 
        Bogus
            .Faker<Location>()
            .CustomInstantiator( fun f -> 
            {
                address = f.Address.FullAddress()
                people = personFaker.GenerateForever() 
                    |> Seq.take(3) 
                    |> Seq.toArray
                id = f.IndexGlobal
            }
        )

    type Company = {
        id: int
        name: string
        business: string
        locations: Location array
    }
    let companyFaker = 
        Bogus
            .Faker<Company>()
            .CustomInstantiator( fun f ->
                { 
                    id = f.IndexGlobal
                    name = f.Company.CompanyName()
                    business = f.Commerce.ProductName()
                    locations = locationFaker.GenerateForever()
                        |> Seq.take(200) 
                        |> Seq.toArray
                    })


    type Directory = Company array

    let init = companyFaker.GenerateForever() |> Seq.take 5

    /// Check if the ids are the same
    let inline replaceIfSame (newItem:'a) (oldItem:'a) = if oldItem.id = newItem.id then newItem else oldItem 
    let inline updateItems (items:'a seq) (item:'a) = items |> Seq.map ( replaceIfSame item )

