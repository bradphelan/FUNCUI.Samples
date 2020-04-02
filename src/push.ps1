$apikey = [IO.File]::ReadAllText("$PSScriptRoot\..\nuget.apikey.secure.txt").Trim()

rm packages\*.nupkg
dotnet pack -o .\packages\ .\XTargets.Elmish.Lens\XTargets.Elmish.Lens.fsproj
dotnet pack -o .\packages\ .\XTargets.FuncUI\XTargets.FuncUI.fsproj
$packages = get-childitem $PSScriptRoot/packages/*.nupkg
foreach ($item in $packages) {
    echo "-----------------------------------"
    echo "Pushing package $item with $apikey"
    echo "-----------------------------------"
    dotnet nuget push $item -k $apikey --source https://api.nuget.org/v3/index.json 
}