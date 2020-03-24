namespace XTargets

open Elmish
open Aether

module Elmish =
    /// A function that maps a value to a value of the same type
    type Update<'State> = 'State -> 'State

    /// A message to with instruction  to
    type Message<'State> = Message of (Update<'State> * Cmd<Message<'State>>)

    module Message =
        let ofUpdate (u: Update<'State>): Message<'State> =
            let cmd: Cmd<Message<'State>> = Cmd.none
            Message(u, cmd)

        let ofUpdatedDeferred (u: Update<'State>): Message<'State> =
            let cmd = Cmd.ofMsg (ofUpdate u)
            Message(id, cmd)

        let ofValue (value: 'State): Message<'State> = (fun _ -> value) |> ofUpdate

        let ofValueDeferred (value: 'State): Message<'State> = (fun _ -> value) |> ofUpdatedDeferred

        let ofNone =
            let cmd: Cmd<Message<'State>> = Cmd.none
            Message(id, Cmd.none)

        let ofUpdateAndCommand (u: Update<'State>) (cmd: Cmd<Message<'State>>) = Message(u, cmd)

        let ofCommand (cmd: Cmd<Message<'State>>) = Message(id, cmd)

        let ofTask action value success =
            let cmd = Cmd.OfTask.perform action value success
            ofCommand cmd

        let ofActionOnThreadPool action success =
            let review =
                fun () ->
                    async { return action() }
                    |> Async.StartAsTask
                    |> Async.AwaitTask

            let update v =
                ofUpdate (fun x ->
                    success (v)
                    x)

            ofCommand (Cmd.OfAsync.perform review () update)




    /// A function that transforms an updater from a
    /// type lower in the tree to a type higher in
    /// the tree.
    type UpdateTransformer<'A, 'B> = Update<'A> -> Update<'B>

    type Dispatch<'State> = Message<'State> -> unit

    module Dispatch =
        // Wrap a dispatcher for a lower level
        let wrap
            (mapper: UpdateTransformer<'ChildState, 'ParentState>)
            (dispatch: Dispatch<'ParentState>) // this dispatcher for the parent level
            : Dispatch<'ChildState>
            =
            let rec fn (m: Message<'ChildState>) =
                match m with
                | Message(update, cmd) ->
                    let mapper' = mapper update
                    let cmd' = Cmd.map fn cmd // note this is recursive :)
                    Message.ofUpdateAndCommand mapper' cmd'
            fn >> dispatch

    /// A type that holds a value and a dispatcher
    /// for updating that value
    type Lens<'State when 'State: equality> =
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
        member x.Focus ((getter:'State ->'Val),(setter: 'Val -> 'State -> 'State) ) : Lens<'Val>  =
            let updateTransformer (itemTransformer:'Val->'Val) (state:'State) =
                setter (getter state |> itemTransformer) state

            let value() = getter x.Get

            let dispatch' = Dispatch.wrap updateTransformer x.dispatch
            { value = value ; dispatch = dispatch' }

        /// Generate a new lens for a two way transformation of the parent lens.
        /// Note that it might not be possible to convert back from the value.
        /// In this case the old value will be used
        member x.Morph((getter:'State->'Val),(setter: 'Val -> 'State option)) : Lens<'Val> =
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
            let each (lens:Lens<'State array>) =
                lens.Get
                    |> Seq.indexed
                    |> Seq.map ( fun (id,_) ->  lens.Focus(at id))
                    |> Seq.toArray

        [<AutoOpen>]
        module Operators = 
            let inline (>->) (root:Lens<'State>) (child:(('State ->'Val)*( 'Val -> 'State -> 'State) )) =
                root.Focus child
            let inline (>?>) (root:Lens<'State>) (child:(('State->'Val)*('Val->'State option))) =
                root.Morph child


        
