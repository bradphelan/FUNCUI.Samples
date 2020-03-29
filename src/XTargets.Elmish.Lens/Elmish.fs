namespace XTargets.Elmish

open Elmish

module Program =
    /// Make a program with some an initial state and a view factory that will accept
    /// an Image to the initial state. 
    let inline mkLensProgram (state:'State) (view:Redux<'State>->'IView) =
        // Process commands at the top level so discard them
        // to meet the type signiture of mkProgram
        let update update state = 
            update state,Cmd.none

        // Wrap the mainview view function and inject the bin file reviewer action
        let view state dispatch = 
            let getter = fun() -> state
            view (Redux(getter,dispatch))

        Elmish.Program.mkProgram (fun () -> state,Cmd.none) update view