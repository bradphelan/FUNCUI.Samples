namespace XTargets.Elmish

open System
open Avalonia
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Components.Hosts
open System.Reactive.Disposables
open FSharp.Control.Reactive


[<Sealed>]
type LocalStateView<'state, 'localState when 'localState : equality>() as this =
    inherit HostControl()
    
    let mutable subscription : IDisposable = null
    let mutable _state : 'state = Unchecked.defaultof<'state>
    let mutable _localState : 'localState = Unchecked.defaultof<'localState>
    let mutable _viewFunc : voption<( Redux<'localState> -> IView)> = ValueNone

    let rec _localStateRedux =
        let getter() = this.LocalState
        let update msg =
            this.LocalState <- msg _localState
        Redux(getter, update)
    
    static let stateProperty =
        AvaloniaProperty.RegisterDirect<LocalStateView<'state, 'localState>, 'state>(
            "State",
            (fun control -> control.State),
            (fun control value -> control.State <- value)
        )
        
    static let localStateProperty =
        AvaloniaProperty.RegisterDirect<LocalStateView<'state, 'localState>, 'localState>(
            "LocalState",
            (fun control -> control.LocalState),
            (fun control value -> control.LocalState <- value)
        )
        
    static let viewFuncProperty =
        AvaloniaProperty.RegisterDirect<LocalStateView<'state, 'localState>, (Redux<'localState> -> IView) voption>(
            "ViewFunc",
            (fun control -> control.ViewFunc),
            (fun control value -> control.ViewFunc <- value)
        )
        
    member this.State
        with get () : 'state = _state
        and set (value: 'state) = this.SetAndRaise(LocalStateView<'state, 'localState>.StateProperty, &_state, value) |> ignore
            
    member this.LocalState
        with get () : 'localState = _localState
        and set (value: 'localState) = this.SetAndRaise(LocalStateView<'state, 'localState>.LocalStateProperty, &_localState, value) |> ignore
            
    member this.ViewFunc
        with get () : voption<(Redux<'localState> -> IView)> = _viewFunc
        and set (value) = this.SetAndRaise(LocalStateView<'state, 'localState>.ViewFuncProperty, &_viewFunc, value) |> ignore  
        
    override this.OnAttachedToVisualTree _locaState =
        let onNext _ =
            let nextView =
                match this.ViewFunc with
                | ValueSome func ->
                    func _localStateRedux
                    |> Some
                | ValueNone -> None
                
            (this :> IViewHost).Update nextView

        let s0 =
            this.GetObservable(LocalStateView<'state, 'localState>.StateProperty)
            |> Observable.map(ignore)
        let s1 =
            this.GetObservable(LocalStateView<'state, 'localState>.LocalStateProperty)
            |> Observable.map(ignore)

        subscription <- 
            Observable.merge s0 s1 
            |> Observable.throttle (TimeSpan.FromMilliseconds 30.0) 
            |> Observable.observeOnContext Threading.SynchronizationContext.Current
            |> Observable.subscribe onNext
        
    override this.OnDetachedFromLogicalTree _locaState =
        if not (isNull null) then
            subscription.Dispose()
            subscription <- null
        
    static member StateProperty = stateProperty
    
    static member LocalStateProperty = localStateProperty
    
    static member ViewFuncProperty = viewFuncProperty


[<AutoOpen>]
module LocalStateView =  
    open Avalonia.FuncUI.Builder
    type LocalStateView<'state, 'localState when 'localState : equality> with
        static member create<'state, 'localState when 'localState : equality>(attrs: IAttr<LocalStateView<'state, 'localState>> list): IView<LocalStateView<'state, 'localState>> =
            ViewBuilder.Create<LocalStateView<'state, 'localState>>(attrs)
            
        static member localState(value: 'localState) : IAttr<LocalStateView<'state, 'localState>> =
            AttrBuilder<LocalStateView<'state, 'localState>>.CreateProperty<'localState>(LocalStateView<'state, 'localState>.LocalStateProperty, value, ValueNone)
            
        static member state(value: 'state) : IAttr<LocalStateView<'state, 'localState>> =
            AttrBuilder<LocalStateView<'state, 'localState>>.CreateProperty<'state>(LocalStateView<'state, 'localState>.StateProperty, value, ValueNone)
            
        static member viewFunc(value: (Redux<'localState> -> IView) voption) : IAttr<LocalStateView<'state, 'localState>> =
            AttrBuilder<LocalStateView<'state, 'localState>>.CreateProperty<_>(LocalStateView<'state, 'localState>.ViewFuncProperty, value, ValueNone)
            
        static member viewFunc(value: Redux<'localState> -> IView) : IAttr<LocalStateView<'state, 'localState>> =
            value |> ValueSome |> LocalStateView<'state, 'localState>.viewFunc