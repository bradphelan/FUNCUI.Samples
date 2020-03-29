namespace XTargets.Elmish

/// A type that holds a value and a dispatcher
/// for updating that value. Why is it called an 
/// 'Image'? When you focus a lens on an object
/// you get an image. Probably a terrible analogy.
type Redux<'State when 'State : equality>(value:unit->'State, dispatch:Message<'State>->unit) =

    let value = value
    let dispatch = dispatch

    member x.Get with get() = value()
    member x.Set(newValue: 'State) =
        newValue
        |> Update.ofValue
        |> dispatch
        ()

    member x.Update(update:'State->'State) =
        Update.ofUpdate update |> dispatch

    member x.Update(update:'State->'State Async) =
        let update' v =
            async {
                let context = System.Threading.SynchronizationContext.Current
                let! r = update v
                do! Async.SwitchToContext context
                Update.ofValue r |> dispatch
            } |> Async.StartImmediate
            v
        Update.ofUpdate update' |> dispatch

    /// Generate new lens for child data of the parent lens
    member x.Focus ((getter,setter): Lens<'State,'Val> ) : Redux<'Val>  =

        let value() = getter x.Get

        let messageUpdater (update:Message<'Val>) = 
                (
                    let update' (state:'State) =
                        let v = getter state 
                        let v' = update v 
                        let state' = setter v' state 
                        state'
                    Update.ofUpdate update'
                )

        let dispatch' msg = messageUpdater msg |> dispatch

        Redux(value, dispatch')

    member x.FocusAsync ((getter,setter):LensAsync<'State,'Val>   ) : Redux<'Val>  =
        let value() = getter x.Get

        // for example 'Val is string
        let messageUpdater (update:Message<'Val>) = 
                (
                    let update' = fun (state:'State) -> 
                        let v = getter state 
                        let v' = update v 
                        async {
                            let context = System.Threading.SynchronizationContext.Current
                            let! state' = setter v' state 
                            do! Async.SwitchToContext context
                            Update.ofValue state' |> dispatch
                        } |> Async.StartImmediate
                        state
                    Update.ofUpdate update'
                )

        let dispatch' msg = messageUpdater msg |> dispatch

        Redux(value,dispatch')

    /// Generate a new lens for a two way transformation of the parent lens.
    /// Note that it might not be possible to convert back from the value.
    /// In this case the old value will be used
    member x.Morph((getter,setter):Isomorphism<'State,'Val>) : Redux<'Val> =
        let setter' v _ = setter v 
        x.Focus(getter,setter')

    member x.MorphAsync((getter,setter):IsomorphismAsync<'State,'Val>) : Redux<'Val> =
        let setter' v _ = setter v 
        x.FocusAsync(getter,setter')

    member x.ToOption (defaultValue:'State) : Redux<'State option> =
        let getter' state  = Some state
        let setter' (v:'State option) (s:'State) : 'State =
            match v with 
            | Some v' -> v'
            | None -> defaultValue
        x.Focus(getter',setter')

    /// Generate a new lens for a two way transformation of the parent lens.
    /// In the case of error the error lens is updated with the error
    member x.Convert (error:'Error option -> unit ) ((getter:Transform<'State,'Val>,setter):Epimorphism<'State,'Val,'Error>) : Redux<'Val> =
        let getter' = getter
        let setter' (v:'Val) (s:'State) =
            match setter v with 
            | Ok o -> 
                None |> error
                o
            | Error err -> 
                Some err |> error
                s
        x.Focus(getter',setter')


    /// Generate a new lens for a two way transformation of the parent lens.
    /// In the case of error the error lens is updated with the error
    member x.ConvertAsync (error:Redux<'Error option>) ((getter,setter):EpimorphismAsync<'State,'Val,'Error>) : Redux<'Val> =

        let setter' v state = 
            async {
                let context = System.Threading.SynchronizationContext.Current
                let! v' = setter v
                do! Async.SwitchToContext context
                match v' with 
                | Ok state' -> 
                    None |> error.Set 
                    return state'
                | Error e -> 
                    Some e |> error.Set
                    return state
            }
        x.FocusAsync(getter,setter')


module Redux =
    /// Create a value that can never change. It
    /// just ignores all values set
    let ofConst c =
        let dispatch update = ()
        let getter = fun()->c
        Redux(getter, dispatch)

    /// Create a value that can be changed
    let ofValue c =
        let mutable data = c
        let dispatch update = data <- update data
        let getter = fun()->data
        Redux(getter, dispatch)

    let ofNone<'a when 'a : equality> : Redux<'a option> =
        ofValue None

    let ofSome v = ofValue (Some v)

type Redux<'State when 'State:equality> with
    static member (>->) ((root:Redux<'State>), (child:Lens<'State,'Val>)) =
        root.Focus child