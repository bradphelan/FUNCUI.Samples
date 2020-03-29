namespace XTargets.Elmish

module Lens =

    module Error =
        let inline toOption (a:Redux<'a*bool> ) : Redux<'a option> =
            let getter() = a.Get |> fst |> Some
            let dispatch' updater =
                let av = getter() |> updater
                match av with 
                | Some k -> a.Set(k,true) // dispatch the new value and a flag that there is no error
                | None -> a.Set(a.Get|>fst,false) // dispatch the old value and a flag that this is an error
            Redux(getter, dispatch')

    module Tuple = 
        let inline mk2 (a:Redux<'a> ) (b:Redux<'b>) : Redux<'a*'b> =
            let getter() = (a.Get,b.Get)
            let dispatch' updater =
                let (av,bv)= getter() |> updater
                a.Set av
                b.Set bv
            Redux(getter, dispatch')

        let inline fst (t:Redux<'a*'b>) =
            let getter v = fst v
            let setter v (a,b) = (v,b)
            t.Focus(getter,setter) 

        let inline snd (t:Redux<'a*'b>) =
            let getter v = snd v
            let setter v (a,b) = (a,v)
            t.Focus(getter,setter) 

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

        let toArray =
            let getter s = [|s|]
            let setter (v:'a array) s = v.[0]
            getter,setter 

        let fromOption =
            let getter = function
                | Some a -> [a]
                | None -> []
            let setter (v:'a array) s =
                match v |> Array.truncate 1 with
                | [|x|] -> Some x
                | _ -> None
            getter,setter

        let toOption<'a> =
            let getter (v:'a array) =
                match v |> Array.truncate 1 with
                | [|x|] -> Some x
                | _ -> None
            let setter (v:'a option) (s:'a array) : 'a array =
                match v with
                | Some a -> [|a|]
                | None -> [||]
            getter,setter


        /// <![CDATA[[Map a Lens<'a array> to Lens<'a> array]]>
        /// <param name="compare">When replacing one item with another this returns true if the item should be replaced with the changed item</param>
        /// <param name="lens">The lens to the original array</param>
        let each (lens:Redux<'State array>) =
            lens.Get
                |> Seq.indexed
                |> Seq.map ( fun (id,_) ->  lens.Focus(at id))
                |> Seq.toArray


