namespace BGS

open Elmish
open XTargets.Elmish
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.FuncUI.Components.Hosts
open FSharpx
open System.IO


type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "Brad Gone Surfing"
        base.Width <- 900.0
        base.Height <- 400.0
        
        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        Result.result {
            // Initialize the main view state
            let initialState = CompaniesView.init Data.init

            // Wrap the mainview view function and inject the bin file reviewer action
            let view state dispatch = 
                let getter = fun() -> state
                CompaniesView.view (Lens.init getter dispatch) 

            // Process commands at the top level so discard them
            // to meet the type signiture of mkProgram
            let update msg state = 
                match msg with
                | Message update ->
                    update state,Cmd.none

            // Start the program
            Elmish.Program.mkProgram (fun () -> initialState,Cmd.none) update view
                |> Program.withHost this
               // |> Program.withConsoleTrace
                |> Program.run
            return ()
        }
        |> ( function
            | Result.Ok _ -> ()
            | Result.Error msg -> failwith msg
        )
        
type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =

        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)