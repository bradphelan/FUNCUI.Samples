namespace BGS
open Bogus


module Data =

    type Person = {
        id: int 
        firstName: string
        lastName: string
    } with
        static member id' = (fun o->o.id),(fun v o -> {o with id = v})
        static member firstName' = (fun o->o.firstName),(fun v o -> {o with firstName = v})
        static member lastName' = (fun o->o.lastName),(fun v o -> {o with lastName = v})

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
        revenue: int32
    } with
        static member id' = (fun o->o.id),(fun v (o:Company) -> {o with id = v})
        static member name' = (fun o->o.name),(fun v o -> {o with name = v})
        static member business' = (fun o->o.business),(fun v o -> {o with business = v})
        static member employees' = (fun o->o.employees),(fun v o -> {o with employees = v})
        static member revenue' = (fun o->o.revenue),(fun v o -> {o with revenue = v})


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
                    revenue = 0
                    })


    type Directory = Company array

    let init = companyFaker.GenerateForever() |> Seq.take 5 |> Seq.toArray

    /// Check if the ids are the same
    let inline getId (x:^a) = (^a : (member id : int)(x))

    let inline replaceIfSame (newItem:'a) (oldItem:'a) = 
        if getId(oldItem) = getId(newItem) then newItem else oldItem 
    let inline updateItems (items:'a seq) (item:'a) = 
        items |> Seq.map ( replaceIfSame item )

