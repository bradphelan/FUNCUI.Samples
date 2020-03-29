namespace XTargets.Elmish
open System

module ValueConverters = 

    let inline private mkParser (p:string->bool*'a) = (
        ( fun (v:'a) -> v.ToString()), 
          fun (txt:string) -> 
            let r,v = p txt
            if r then
                Result.Ok v
            else
                Result.Error (sprintf "failed to parse %s as Int32" txt)
        )

    let StringToInt16 = mkParser Int16.TryParse
    let StringToInt32 = mkParser Int32.TryParse
    let StringToInt64 = mkParser Int64.TryParse
    let StringToFloat = mkParser Double.TryParse


    let handleErrors (getter,setter) errHandler =
        let setter' = function
            | Ok v -> Some v
            | Error e -> 
                errHandler e
                None 
        (getter, setter)