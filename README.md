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
| Name                         | Type              | Default       | Description   |
| ---------------------------- | ----------------- | ------------- | ------------- |
| `SaveCustomFiles`            | Boolean           | `true`        | <p>When opening a mixtape in the editor with custom files, save these files with the mixtape whenever the mixtape is saved.</p> |
| `LogFileLoading`             | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose file loading of custom files in .bop archives.</p> |
| `LogUnloading`               | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose custom asset unloading.</p> |
| `LogSeparateTextureSprites`  | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose custom sprite creation from separate textures.</p> |
| `LogAtlasTextureSprites`     | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose custom sprite creation from atlas textures.</p> |
| `LogSceneIndices`            | BepInEx.LogLevel  | `None`        | <p>Log level for vanilla scene loading, including scene name + build index.</p> <p>Useful when you need to rip sprites from a sprite atlas, which requires knowing the build index of a game to locate its sharedassets file.</p> |

## Building 
### Prerequisites
- Bits & Bops v1.6+
- Microsoft .NET SDK v4.7.2+
- Visual Studio 2022 (Optional)

### Steps
1. Clone this repository using ``git clone https://github.com/AnonUserGuy/BopCustomTextures.git``.
2. From ``<Bits & Bops installation>/Bits & Bops_Data/Managed/``, copy ``Assembly-CSharp.dll`` and ``Unity.TextMeshPro.dll`` into ``BopCustomTextures/lib/``.
3. Build
    - Using CLI:
      ```bash
      dotnet restore BopCustomTextures.sln
      dotnet build BopCustomTextures.sln
      ```
    - Using Visual Studio 2022:
       - Open BopCustomTextures.sln with Visual Studio 2022.
       - Set build mode to "release".
       - Build project.
4. Copy ``BopCustomTextures/BopCustomTextures/bin/Release/net472/BopCustomTextures.dll`` into ``<Bits & Bops Installation>/BepinEx/plugins/``.

### Configuration (Optional)
You can setup `BopCustomTextures.csproj.user` file next to `BopCustomTextures.csproj` with the `PostBuildCopyDestination` path set to automatically copy the new DLL after build:
```xml
<Project>
  <PropertyGroup>
    <PostBuildCopyDestination>&lt;Bits &amp; Bops Installation&gt;/BepInEx/plugins</PostBuildCopyDestination>
  </PropertyGroup>
</Project>
```

## Implementation
### Custom Textures
In Unity, texture assets can only be directly replaced by hooking into the asset loading pipeline. Using this to replace assets is unreliable however, as if an asset was already loaded before being required by a mixtape containing custom assets, the already loaded assets won't be replaced as they don't need to go through the asset loading pipeline again. So, instead of replacing the texture assets directly, instead every GameObject with a sprite renderer in a rhythm game with custom textures is given a new component, "CustomSpriteSwapper", that checks every frame if the sprite renderer has updated sprites and, when it does, replaces the sprite with a custom one if a custom one exists. 

This is similar to the approach the game itself uses for swapping sprites such as in rhythm games like "Molecano" and "Flow Worms", plus it also enables it to work in any rhythm game without any respect to the rhythm game itself.
### Scene Mods
The scene mod implementation is comparatively much simpler than the custom texture system. During rhythm game loading for a custom mixtape, when a rhythm game finishes loading the corresponding JSON file included in the .bop archive is iterated through, searching for GameObjects and components using the syntax given previously. If the GameObject and component can be found the component is updated with the custom values included in the JSON, and otherwise a warning message is printed to the console informing the mixtape author of the missing GameObject or component. 

The process requires implementing assignment methods for every individual Unity component which is less than ideal, but there's not much way around it without completely circumventing normal scene management in the Unity runtime.    

## Todo
- Implement custom AssetBundle loading.
  - Would allow custom animations (?), more precise sprite attributes, and otherwise better image compression for custom textures.
- Implement metadata files to define custom sprite attributes.
  - Short of custom AssetBundle loading, would allow mixtape files to control sprite attributes like size and position without the current bodge-y method of increasing the base sprite canvas on all sides.
- Implement more supported components for scene mods.
  - While every component being fully modifiable would be nice, implementing this would take lots of effort programming and testing. As such, component support will likely be added as the community sees need for it. (That is to say, if you want to do something with scene mods that isn't currently possible, don't be afraid to make a request for it!)
