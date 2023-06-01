# Platforms

* Unity 2021.3.20 +

#  Release

​	The best way to launch, Download [UnityPackage](https://github.com/K1einB1ue/KolidSoft.Json/releases).

# Installation

1. Open or create Unity project

2. Import asset into project:

   i.   copy `Editor  `, `Packages`into your project Assets folder. 

   ii. if you have deployed an `.unitypackage` - import it in Unity Editor by selecting `Import Package` → `Custom Package`

# Usage 

1. `Editor` includes `UI` build with `ui-tool kit` . path is hard coded in `Packages/KolidJson/Config/ConfigPath.cs`, if you want to modify, then go check it.

2.  Create `Scenes` , `SaveFiles`directory.

3.  Binding the directory through  `JsonSoft` → `Json/ConfigWindow` ,

    `RootPath`  to `SaveFiles`,`ScenePath` to `Scenes`. (absolute path)

4.  Select `SaveFile` then enter `SaveFileName`. press `Create` Button

5.  Use `JsonSoft` → `Json/ObjectWindow` to modify `JsonObject`,

   `JsonSoft` → `Json/TypeWindow` to modify `JsonObject.JsonQuery(string key)`
6.  If you are using `Adressables`, you have to set `BuildScriptPackedMode.s_SkipCompilePlayerScripts = true;`
