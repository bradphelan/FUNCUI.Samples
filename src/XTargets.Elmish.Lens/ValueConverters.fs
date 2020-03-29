namespace XTargets.Elmish
open System

module ValueConverters = 

    let inline private mkValueConverter (p:string->bool*'a) = (
        ( fun (v:'a) -> v.ToString()), 
          fun (txt:string) -> 
            let r,v = p txt
            if r then
                Result.Ok v
            else
                Result.Error (sprintf "failed to parse %s as Int32" txt)
        )

    let StringToInt16 = mkValueConverter Int16.TryParse
    let StringToInt32 = mkValueConverter Int32.TryParse
    let StringToInt64 = mkValueConverter Int64.TryParse
    let StringToFloat = mkValueConverter Double.TryParse
