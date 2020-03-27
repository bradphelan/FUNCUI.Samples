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
        base.Title <- "Validating text box"
        base.Width <- 400.0
        base.Height <- 80.0
        
        //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
        //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

        Result.result {
            // Initialize the main view state
            let initialState = Data.Item.init
            let initialViewState = ItemView.State.init

            // Start the program
            Program.mkLensProgram (initialViewState,initialState) ItemView.view 
                |> Program.withHost this
                |> Program.withConsoleTrace
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

        let result = 
            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .StartWithClassicDesktopLifetime(args)

        result 