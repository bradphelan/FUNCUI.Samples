namespace XTargets.Elmish

/// A function that transforms one type into another type
type Transform<'a,'b>='a->'b
/// A function that maps a value to a value of the same type
type Update<'a> = Transform<'a,'a>
/// A function that maps a value to a value of the same type
type PartialUpdate<'a,'b>='a->'b->'b

/// A function that generates values when called
type Generator<'a> = unit->'a

/// A function that transforms one type into another type
type TransformAsync<'a,'b> = 'a->Async<'b>
/// A function that asynchronously maps a value to a value of the same type
type UpdateAsync<'a> = TransformAsync<'a,'a>
/// A function that asynchronously maps a value to a value of the same type
type PartialUpdateAsync<'a,'b> = 'a->'b->Async<'b>

/// Access to a subset of the data in state
type Lens<'a,'b> = Transform<'a,'b>*PartialUpdate<'b,'a>
/// A lens that has async update behaviour. 
type LensAsync<'a,'b> = Transform<'a,'b>*PartialUpdateAsync<'b,'a>

/// A two way transformation 
type Isomorphism<'a,'b> = Transform<'a,'b>*Transform<'b,'a>
/// An async two way transformation
type IsomorphismAsync<'a,'b> = Transform<'a,'b>*TransformAsync<'b,'a>

/// Like a morpher but might fail when converting 'Val to 'State
type Epimorphism<'a,'b,'error> = Transform<'a,'b>*Transform<'b,Core.Result<'a,'error>>
/// An async two way transformation that might fail
type EpimorphismAsync<'a,'b,'error> = Transform<'a,'b>*Transform<'b,Core.Result<'a,'error> Async>

/// A message to with instruction  to
type Message<'State> = Update<'State>


module Update =
    let ofUpdate (u: Update<'State>): Message<'State> =
        u

    let ofValue (value: 'State): Message<'State> = 
        (fun _ -> value) |> ofUpdate

    let ofNone =
        id