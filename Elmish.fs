
namespace BGS
open Elmish

module Elmish =
    /// A function that maps a value to a value of the same type
    type Update<'State> = ('State->'State)

    /// A message to with instruction  to 
    type Message<'State> = Message of (Update<'State>*Cmd<Message<'State>>)
    module Message =
        let ofUpdate (u:Update<'State>) : Message<'State> = 
            let cmd:Cmd<Message<'State>> = Cmd.none
            Message (u,cmd)

        let ofValue (value:'State) : Message<'State> =
            (fun _ -> value) |> ofUpdate

        let ofNone =
            let cmd:Cmd<Message<'State>> = Cmd.none
            Message (id, Cmd.none)

        let ofUpdateAndCommand (u:Update<'State>) (cmd:Cmd<Message<'State>>)  =
            Message (u,cmd)

        let ofCommand (cmd:Cmd<Message<'State>>) =
            Message(id,cmd)

        let ofTask action value success =
            let cmd = Cmd.OfTask.perform action value success
            ofCommand cmd 

        let ofActionOnThreadPool action success =
            let review = fun () -> 
                async { return action() } 
                |> Async.StartAsTask 
                |> Async.AwaitTask
            let update v = ofUpdate ( fun x -> success(v);x)
            ofCommand (Cmd.OfAsync.perform review () update)


    /// A function that transforms an updater from a
    /// type lower in the tree to a type higher in
    /// the tree.
    type UpdateTransformer<'A,'B> = Update<'A>->Update<'B>

    type Dispatch<'State> = Message<'State>->unit
    module Dispatch =
        // Wrap a dispatcher for a lower level
        let wrap 
            (mapper:UpdateTransformer<'ChildState,'ParentState>)
            (dispatch:Dispatch<'ParentState>)     // this dispatcher for the parent level 
            : Dispatch<'ChildState> =                // returns a lower level dispatcher
            let rec fn (m:Message<'ChildState>) =
                match m with
                | Message (update, cmd) ->
                    let mapper' = mapper update
                    let cmd' = Cmd.map fn cmd   // note this is recursive :)
                    Message.ofUpdateAndCommand mapper' cmd'
            fn >> dispatch

    /// A type that holds a value and a dispatcher
    /// for updating that value
    type Lens<'State> when 'State : equality = { 
        value: 'State
        dispatch: Dispatch<'State>
    }
    with 
        member x.Set(newValue:'State) =
            if newValue<>x.value then
                newValue |> Message.ofValue |> x.dispatch
            else
                ()
        member x.Focus (updater:'Val->'State->'State) (item:'Val) = 
            let updateTransformer itemTransformer = itemTransformer item |> updater
            let dispatch' = Dispatch.wrap updateTransformer x.dispatch
            {value=item; dispatch=dispatch'}


    module Lens = 
        let init value dispatch = 
            {value=value; dispatch=dispatch}

        /// Generate a lens given an item, it's parent state and a way to update that item
        let returnM (parentLens:Lens<'a>) stateUpdater item =
            parentLens.Focus stateUpdater item 

            
