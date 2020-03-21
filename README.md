# XTargets.Elmish.Lens

A working demo for using a lens with the [FuncUI](https://github.com/AvaloniaCommunity/Avalonia.FuncUI) [Elmish API](https://github.com/elmish/elmish) for [Avalonia](https://github.com/AvaloniaUI/Avalonia) rather than the classic elmish messages and commands.

*May* also work with other Elmish consumers such as Fable?? There is no dependency directly on FUNCUI.

https://github.com/bradphelan/FUNCUI.Samples

![image](https://user-images.githubusercontent.com/17650/77196497-c72da800-6ae3-11ea-8f6a-f4a13fc6f853.png)

It's a master child editor setup. The data model is

```
Application has_many  Companies
Company has_many Employees
```

The view on the left allows you to select a company. The view on the right allows you to edit the company name and product and also edit ( but not yet add or remove employees )

Troubles I had.

*  TextBox.OnTextChanged is a PITA. Because it fires when the model is first loaded without user input it often triggers the dispatch loop to keep firing forever. This might have been to due to other bugs I had but OnTextChanged is evil. I now listen specifically for keypresses to ensure that an update is user fired rather that UI fired.

* ListBoxes combined with editable TextBoxes don't really work.  The list row is not selected as the EditBox doesn't bubble the tap event back to the ListBox. I fought this for a while and gave up. The UI kind of works around the problem. Notice I don't allow editing of company data in the left hand list view as I need the row selection to work but in the right hand view I do allow it for employees because I don't care about the selection.

![2020-03-20_19-56-11](https://user-images.githubusercontent.com/17650/77197299-3d7eda00-6ae5-11ea-8dd9-32f2a86625ae.gif)


You should be able to build the app with just

```
dotnet build
dotnet run
```

