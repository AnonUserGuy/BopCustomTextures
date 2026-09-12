# BopCustomTextures
A BepInEx mod for Bits & Bops that allows custom mixtape files to include custom textures in their .bop archive. 

The mod was created because I wanted to create a custom mixtape with custom textures, but I also wanted others to be able to create custom mixtapes with custom textures without having to create their own mods.

Demo: https://youtu.be/pZQ74qy7PbY

## Installation
- Install [BepInEx 5.x](https://docs.bepinex.dev/articles/user_guide/installation/index.html) in Bits & Bops.
- Download `BopCustomTextures.dll` from the latest [release](https://github.com/AnonUserGuy/BopCustomTextures/releases/), and place it in ``<Bits & Bops Installation>/BepinEx/plugins/``.

## Usage
For a detailed guide on usage, see the [Bop Custom Textures manual](https://github.com/AnonUserGuy/BopCustomTextures/wiki).

### Configuration
After running Bits & Bops with the latest version of this plugin installed, a configuration file will be generated at `BepinEx\config\BopCustomTextures.cfg`. Open this file with a text editor to access the following configs:
<table><tr><th>Name</th><th>Type</th><th>Default</th><th>Description</th></tr>
<tr><th colspan="4">Editor</th></tr>
<tr><td><code>SaveCustomFiles</code></td><td>Boolean</td><td><code>true</code></td><td>When opening a modded mixtape in the editor, maintain its custom asset files whenever the mixtape is saved.</td></tr>
<tr><td><code>UpgradeOldMixtapes</code></td><td>Boolean</td><td><code>true</code></td><td>When opening a modded mixtape for an older version of the plugin in the editor, upgrade the mixtape version to the current one when saving.</td></tr>
<tr><td><code>UploadAppendDescription</code></td><td>Boolean</td><td><code>true</code></td><td>When uploading a modded mixtape to the Steam Workshop, add a blurb to the end of the description with a link to download BopCustomTextures.</td></tr>
<tr><td><code>LoadOutdatedPluginEditor</code></td><td>Boolean</td><td><code>true</code></td><td>When opening a modded mixtape in the editor made for a newer version of BopCustomTextures, attempt to load custom assets.</td></tr>
<tr><th colspan="4">Editor.Display</th></tr>
<tr><td><code>HijackEventCategory</code></td><td>String</td><td><code>BopCustomTextures</code></td><td><p>If this event category (or any event category in a comma-delineated list) is selected multiple times, it'll cycle through all modded event categories.</p><p>Set to blank ("HijackEventCategory = ") to disable this feature.</p><p>Ex: _, gameManager, effects, accessibility, debug, BopCustomTextures</p></td></tr>
<tr><td><code>DisplayOptionsCopy</code></td><td>Display</td><td><code>Always</code></td><td>When to display "copy from file" and "copy from folder" in "Global Properties".</td></tr>
<tr><td><code>DisplayOptionsReload</code></td><td>Display</td><td><code>Always</code></td><td>When to display "reload" in "Global Properties".</td></tr>
<tr><td><code>DisplayEventTemplates</code></td><td>Display</td><td><code>Always</code></td><td><p>When to display category "Bop Custom Textures".</p><p>Can cause issues with BopVisualEffects if set to anything besides Always.</p></td></tr>
<tr><td><code>EventTemplatesBefore</code></td><td>String</td><td><code></code></td><td><p>If set, will search through comma-delineated list until an event category with the same name is found, inserting "Bop Custom Textures" before that category. If no matches are found, EventTemplatesAfter is used instead.</p><p>Ex: BopVisualEffects, effects</p></td></tr>
<tr><td><code>EventTemplatesAfter</code></td><td>String</td><td><code></code></td><td><p>If set, will search through comma-delineated list until an event category with the same name is found, inserting "Bop Custom Textures" after that category. If no matches are found, EventTemplatesIndex is used instead.</p><p>Ex: BopVisualEffects, effects</p></td></tr>
<tr><td><code>EventTemplatesIndex</code></td><td>Int32</td><td><code>4</code></td><td>Position in categories to display "Bop Custom Textures" at. Values lower than 0 will put category at end of list.</td></tr>
<tr><th colspan="4">Editor.Keybinds</th></tr>
<tr><td><code>CopyCustomsFromFileKeybind</code></td><td>KeyCode</td><td><code>F3</code></td><td>Keybind used to access Copy Customs From File.</td></tr>
<tr><td><code>CopyCustomsFromFolderKeybind</code></td><td>KeyCode</td><td><code>F4</code></td><td>Keybind used to access Copy Customs From Folder.</td></tr>
<tr><td><code>ReloadCustomAssetsKeybind</code></td><td>KeyCode</td><td><code>F5</code></td><td>Keybind used to access Reload Custom Assets.</td></tr>
<tr><td><code>SelectEventCatagoryKeybind</code></td><td>KeyCode</td><td><code>None</code></td><td>Keybind used to switch to "Bop Custom Textures" catagory.</td></tr>
<tr><th colspan="4">General</th></tr>
<tr><td><code>LoadCustomAssets</code></td><td>Boolean</td><td><code>true</code></td><td><p>When opening a modded mixtape, load the custom assets stored in it.</p><p>(Note: modded mixtapes won't maintain their custom files if saved while this is disabled.)</p></td></tr>
<tr><th colspan="4">Logging</th></tr>
<tr><td><code>logOutdatedPlugin</code></td><td>LogLevel</td><td><code>Error</code>, <code>MixtapeEditor</code></td><td>Log level for message indicating BopCustomTextures needs to be updated to play a mixtape.</td></tr>
<tr><td><code>LogUpgradeMixtape</code></td><td>LogLevel</td><td><code>Warning</code>, <code>MixtapeEditor</code></td><td>Log level for messaage reminding user to save a mixtape to add/upgrade its BopCustomTextures.json file.</td></tr>
<tr><th colspan="4">Logging.Debugging</th></tr>
<tr><td><code>LogFileLoading</code></td><td>LogLevel</td><td><code>Debug</code></td><td>Log level for verbose file loading of custom files in .bop archives.</td></tr>
<tr><td><code>LogUnloading</code></td><td>LogLevel</td><td><code>Debug</code></td><td>Log level for verbose custom asset unloading.</td></tr>
<tr><td><code>LogSeperateTextureSprites</code></td><td>LogLevel</td><td><code>Debug</code></td><td>Log level for verbose custom sprite creation from seperate textures.</td></tr>
<tr><td><code>LogAtlasTextureSprites</code></td><td>LogLevel</td><td><code>Debug</code></td><td>Log level for verbose custom sprite creation from atlas textures.</td></tr>
<tr><td><code>LogMComponentRegistering</code></td><td>LogLevel</td><td><code>Debug</code></td><td>Log level for registering of MComponents.</td></tr>
<tr><th colspan="4">Logging.Modding</th></tr>
<tr><td><code>LogSceneIndices</code></td><td>LogLevel</td><td><code>None</code></td><td>Log level for vanilla scene loading, including scene name + build index. (for locating level and sharedassets files)</td></tr>
<tr><th colspan="4">Player</th></tr>
<tr><td><code>LoadOutdatedPluginPlayer</code></td><td>OutdatedPluginHandling</td><td><code>ShowDisclaimer</code></td><td>How to handle opening a modded mixtape in the Mixtape Player that was made for a newer version of BopCustomTextures.</td></tr>
</table>

## Building 
### Prerequisites
- Bits & Bops v1.14+
- Microsoft .NET SDK v4.7.2+
- Visual Studio 2022 (Optional)

### Steps
1. Clone this repository using ``git clone https://github.com/AnonUserGuy/BopCustomTextures.git``.
2. Copy the following from ``<Bits & Bops installation>/Bits & Bops_Data/Managed/`` into ``./lib/``:
    - ``Assembly-CSharp.dll``
    - ``StandaloneFileBrowser.dll``
    - ``TempoStudio.Core.dll``
    - ``Unity.TextMeshPro.dll``
4. Build
    - Using CLI:
      ```bash
      dotnet restore BopCustomTextures.sln
      dotnet build BopCustomTextures.sln
      ```
    - Using Visual Studio 2022:
       - Open BopCustomTextures.sln with Visual Studio 2022.
       - Set build mode to "release".
       - Build project.
5. Copy ``BopCustomTextures/BopCustomTextures/bin/Release/net472/BopCustomTextures.dll`` into ``<Bits & Bops Installation>/BepinEx/plugins/``.

### Configuration (Optional)
You can setup `BopCustomTextures.csproj.user` file next to `BopCustomTextures.csproj` with the `PostBuildCopyDestination` path set to automatically copy the new DLL after build:
```xml
<Project>
  <PropertyGroup>
    <PostBuildCopyDestination>&lt;steam location&gt;/steamapps/common/Bits &amp; Bops/BepInEx/plugins</PostBuildCopyDestination>
  </PropertyGroup>
</Project>
```

## Implementation
### Custom Textures
In Unity, texture assets can only be directly replaced by hooking into the asset loading pipeline. Using this to replace assets is unreliable however, as if an asset was already loaded before being required by a mixtape containing custom assets, the already loaded assets won't be replaced as they don't need to go through the asset loading pipeline again. So, instead of replacing the texture assets directly, instead every GameObject with a sprite renderer in a rhythm game with custom textures is given a new component, "CustomSpriteSwapper", that checks every frame if the sprite renderer has updated sprites and, when it does, replaces the sprite with a custom one if a custom one exists. 

This is similar to the approach the game itself uses for swapping sprites such as in rhythm games like "Molecano" and "Flow Worms", plus it also enables it to work in any rhythm game without any respect to the rhythm game itself.
### Scene Mods
The scene mod implementation is comparatively much simpler than the custom texture system. During rhythm game loading for a custom mixtape, when a rhythm game finishes loading the corresponding JSON file included in the .bop archive is iterated through, searching for GameObjects and components using the syntax given previously. If the GameObject and component can be found the component is updated with the custom values included in the JSON, and otherwise a warning message is printed to the console informing the mixtape author of the missing GameObject or component. 

<s>The process requires implementing assignment methods for every individual Unity component which is less than ideal, but there's not much way around it without completely circumventing normal scene management in the Unity runtime.</s>    

***Reimplementing scene mods using reflection/dynamic code generation could enable scene mods to modify any Unity component independent of implementation, which may be something looked into in the future. As of now however, scene mods can only modify specific supported Unity components.***

## License
Licensed under the MIT License.

This project includes code derived from [BopVisualEffects](https://github.com/Brollyy/BopVisualEffects), licensed under the MIT License.
