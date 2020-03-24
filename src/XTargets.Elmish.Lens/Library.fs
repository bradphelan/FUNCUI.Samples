namespace XTargets

open Elmish
open Aether

module Elmish =
    /// A function that maps a value to a value of the same type
    type Update<'State> = 'State -> 'State
    /// A function that asynchronously maps a value to a value of the same type
    type UpdateAsync<'State> = 'State -> Async<'State>
    /// Access to a subset of the data in state
    type Lens<'State,'Val> = ('State->'Val)*('Val->'State->'State) 
    /// A transformation that always succeeds
    type Morpher<'State,'Val> = ('State->'Val)*('Val->'State) 
    /// Like a morpher but might fail when converting 'Val to 'State
    type Parser<'State,'Val> = ('State->'Val)*('Val->'State option) 

    /// A message to with instruction  to
    type Message<'State> = 
        | Message of (Update<'State>)

    module Message =
        let ofUpdate (u: Update<'State>): Message<'State> =
            Message(u)

        let ofValue (value: 'State): Message<'State> = (fun _ -> value) |> ofUpdate

        let ofNone =
            Message(id)

        // let ofTask (action:unit->'State) =
        //     let foo =
        //         async { return action() }
        //         |> Async.StartAsTask
        //         |> Async.AwaitTask

        //     ofAsync foo

    type Dispatch<'State> = Message<'State> -> unit

    /// A type that holds a value and a dispatcher
    /// for updating that value
    type Image<'State when 'State: equality> =
        { value: unit -> 'State
          dispatch: Dispatch<'State> 
        }
        with
        member x.Get with get() = x.value()
        member x.Set(newValue: 'State) =
            if newValue <> x.value() then
                newValue
                |> Message.ofValue
                |> x.dispatch
            else
                ()

        /// Generate new lens for child data of the parent lens
        member x.Focus ((getter,setter): Lens<'State,'Val> ) : Image<'Val>  =

            let value() = getter x.Get

            let messageUpdater (message:Message<'Val>) = 
                match message with 
                | Message update -> 
                    (
                        let update' = fun (state:'State) -> 
                            let v = getter state 
                            let v' = update v 
                            let state' = setter v' state 
                            state'
                        Message.ofUpdate update'
                    )

            let dispatch' msg = messageUpdater msg |> x.dispatch

            { value = value ; dispatch = dispatch' }

        /// Generate a new lens for a two way transformation of the parent lens.
        /// Note that it might not be possible to convert back from the value.
        /// In this case the old value will be used
        member x.Morph((getter,setter):Morpher<'State,'Val>) : Image<'Val> =
            let setter' v _ = setter v 
            x.Focus(getter,setter')

        /// Generate a new lens for a two way transformation of the parent lens.
        /// Note that it might not be possible to convert back from the value.
        /// In this case the old value will be used
        member x.Parse ((getter,setter):Parser<'State,'Val>) : Image<'Val> =
            let setter' v state = 
                match setter v with 
                | Some state' -> state'
                | None -> state
            x.Focus(getter,setter')


    module Lens =
        let init value dispatch =
            { value = value
              dispatch = dispatch }

        /// Convert a lens of an array to an array of lenses
        /// ie:
        ///  
        /// <![CDATA[ Lens<int array> -> Lens<int> array ]]>

        module Array =
            /// Generate a lens for an array based on matching the array item by some condition
            let find (pred:'a->bool) = 
                let setter (c:'a) (s:'a array) = 
                        s 
                        |> Seq.map ( fun c' -> if pred(c') then c else c') 
                        |> Seq.toArray
                let getter (s:'a array) =
                    s
                    |> Seq.find ( pred )
                getter,setter


            /// Build a lens to the specific item in the array
            let at (index:int)  =
                let setter (c:'a) (s:'a array) = 
                        s 
                        |> Seq.indexed
                        |> Seq.map ( fun (id, c') -> if id = index then c else c') 
                        |> Seq.toArray
                let getter (s:'a array) =
                    s.[index]

                getter,setter


            /// <![CDATA[[Map a Lens<'a array> to Lens<'a> array]]>
            /// <param name="compare">When replacing one item with another this returns true if the item should be replaced with the changed item</param>
            /// <param name="lens">The lens to the original array</param>
            let each (lens:Image<'State array>) =
                lens.Get
                    |> Seq.indexed
                    |> Seq.map ( fun (id,_) ->  lens.Focus(at id))
                    |> Seq.toArray

        [<AutoOpen>]
        module Operators = 
            let inline (>->) (root:Image<'State>) (child:Lens<'State,'Val>) =
                root.Focus child
            let inline (>?>) (root:Image<'State>) (child:Parser<'State,'Val>) =
                root.Parse child


        
