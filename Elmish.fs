namespace BGS

open Elmish

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
        member x.Focus (setter: 'Val -> 'State -> 'State) (getter:'State ->'Val) : Lens<'Val>  =
            let updateTransformer (itemTransformer:'Val->'Val) (state:'State) =
                setter (getter state |> itemTransformer) state

            let value() = getter x.Get

            let dispatch' = Dispatch.wrap updateTransformer x.dispatch
            { value = value ; dispatch = dispatch' }


    module Lens =
        let init value dispatch =
            { value = value
              dispatch = dispatch }

        /// Convert a lens of an array to an array of lenses
        /// ie:
        ///  
        /// <![CDATA[ Lens<int array> -> Lens<int> array ]]>
        let focusArray (compare:'State->'State->bool) (lens:Lens<'State array>) =
            lens.Get
                |> Seq.map ( fun (item:'State) -> 
                    let getter (s:'State array) = 
                        s |> Seq.find(compare item)
                    let setter v (s: 'State array) = 
                        s 
                        |> Seq.map ( fun v' -> if compare v v' then v else v' ) 
                        |> Seq.toArray
                    lens.Focus setter getter 
                )
                |> Seq.toArray

