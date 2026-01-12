# BopCustomTextures
A BepInEx mod for Bits & Bops that allows custom mixtape files to include custom textures in their .bop archive.

Demo: https://youtu.be/pZQ74qy7PbY

## Installation
- Install [BepInEx 5.x](https://docs.bepinex.dev/articles/user_guide/installation/index.html) in Bits & Bops.
- Download `BopCustomTextures.dll` from the latest [release](https://github.com/AnonUserGuy/BopCustomTextures/releases/), and place it in `BepinEx\plugins\`.

## Usage
### File Structure
Adding custom textures to a mixtape requires editing the .bop file outside of Bits & Bops using some sort of file archiver program. Bop files are actually just .zip archives with a different extension, so about any program will do.
 - [7-Zip](https://www.7-zip.org/) is my go to choice for a program like this.
 - [WinRAR](https://www.rarlab.com/download.htm) is another popular choice, although its *technically* paid software.
 - If you're desperate, you can just rename a .bop file to have a .zip extension instead so your file explorer will recognize it as a zip archive. For example, ``my_mixtape.bop`` would become ``my_mixtape.zip``. It might just be more annoying opening the file in Bits & Bops later on if you do so.

With a file archiver program available, now open any .bop file as a file archive. You'll see the following contents:
```
root:
 - mixtape.json
 - song.bin
```
``mixtape.json`` contains all the Bits & Bops specific data for your mixtape, while ``song.bin`` is just the song used in the mixtape with a generic file extension.

To start adding custom textures to the mixtape, add a new folder to the archive named ``textures`` (or ``tex`` if you appreciate brevity)
```
root:
- textures\
- mixtape.json
- song.bin
```

To start adding scene mods (like moving game objects around or recoloring sprite renderers), add a new folder to the archive named ``levels`` (or ``scenes``)
```
root:
- levels\
- textures\
- mixtape.json
- song.bin
```

### Adding Custom Textures
With the ``textures`` folder created, you're about ready to start adding custom textures to your mixtape. But first, you'll need to create a subfolder in ``textures`` corresponding to the game the textures are for. To target a game, you'll need to name the subfolder the internal name of the game. This will always, *always* just be whatever name the game is listed as in the Mixtape Editor without any punctuation or spaces. For example, a non-mixtape game like "Rock, Paper, Showdown!" will become ``RockPaperShowdown``, while a mixtape game like "Flow Worms (Sky)" will become ``FlowWormsSky``.

If you were targetting "Flow Worms (Sky)" and "Rock, Paper, Showdown!", your ``textures`` folder should now contain the following subfolders:
```
textures:
- FlowWormsSky\
- RockPaperShowdown\
```
Within these folders you'll add you're custom textures as PNG and JPEG files. Additional sobfolders within these will all be searched by the plugin, so feel free to add additional subfolders for organization of types of textures.  

From here, you have two options for supplying your custom textures: "atlas" textures and "seperate" textures. 

#### Atlas Textures 
In Bits & Bops, individual sprites like a blue gentleman ordering tea or an ant marching aren't actually stored as individual images, but rather as large sheets of sprites with special metadata associated with them that the Unity engine uses to split into individual sprites at runtime, called sprite atlases. If you want to replace an entire sheet like this, henceforth refered to as an atlas texture, you'll need to name the file appropriately. 

If you directly extract an atlas texture from Bits & Bops's game files, you'll recieve it with a name prefixed with ``sactx-N``, ``N`` being some number specifying which atlas texture it was for the given game. if you name an image file such that it begins with ``sactx-N`` and place it in the appropriate game folder, it will be used as an atlas texture and replace an entire sprite atlas's texture.

***Note: For the time being, I DO NOT recommend using atlas textures. It takes a very long time to regenerate the sprites to use the new atlas texture (~10s for a small game like "Octeaparty (Fire)"). Until the process can be optimized, you will probably prefer to work with individual textures instead.***

#### Seperate Textures
If, instead of replacing an entire atlas texture, you just want to replace one individual sprite, (or you are heeding my suggestion at the end of the previous section,) you can instead supply individual textures. Individual textures correspond to the individual sprites contained within a sprite atlas. If you give an image file the same name as a sprite's name and place it in the appropriate game folder, it will be used as a seperate texture and replace the texture of a single sprite. 
 - IF you're downloading textures from [the Spriters Resource](https://www.spriters-resource.com/pc_computer/bitsbops/), sprites are already given in this format.
 - If you can't find a desired sprite there, I found [UnityPy](https://pypi.org/project/UnityPy/1.5.1/) to be the easiest way to extract them manually.

#### Using Textures with Different Dimensions from Base Texture
Supplying a texture with different dimensions from the base texture is handled differently depending on what kind of custom texture you're using. 
 - If you're using an atlas texture, the custom texture will be scaled to fit the dimensions of the old texture. This way you can downscale your base textures and they will still work.
 - If you're using a seperate texture, the custom texture will be treated as if you expanded the canvas size of the base texture with it centered. Any extra bounds will be added on all sides of the image equally. This way, if you wanna add something to a sprite outside of the bounds of its texture, you can simply expand it on all sides such that the additions now fit on the canvas.

#### Optimizing for Size
Most of Bits & Bops's sprites are illustrated for 1080p viewing, and Bits & Bops has a lot of sprites. As such, you will probably soon notice the file size of your .bop file ballooning due to all these custom textures. Here I have a couple of suggestions for reducing file size:
 - For atlas textures, you can simply downscale them. Unfortunately this isn't an option for seperate textures because they handle altered texture dimensions in a different way.
 - If a texture contains little to no transparency (like textures for backgrounds or very rectangular sprites) you can convert the image to a highly compressed JPEG.
 - Optimize the PNG texture.
 - Reduce the amount of colors in the PNG texture.
 - If you can, perform your modifications using a scene mod. 

### Adding Scene Mods
A more fledgling feature of this plugin is the ability to modify the scenes of rhythm games, a feature dubbed "scene mods". Scene mods can be used to move game objects around, change the tint color of sprite renderers, etc. Scene mods are described using JSON, with the following basic structure:
```js
{
  "parent_object/child_object": {
    "!Transform": {
      "LocalPosition": {
        "x": 0.0 // new X-coordinate position
        // other coordinate positions unmodified
      }
    },
    "grandchild_object": {
      "!SpriteRenderer": {
        "Color": {
          // makes it magenta
          "r": 1.0,
          "g": 0.0,
          "b": 1.0
        }
      }
    }
  }
}
```
Within the root JSON object you can specify the name of a root GameObject (not the child of any other game object in the scene) to modify. You can also specify the path of any GameObject relative to the scene using slashes, hence ``"parent_object/child_object"``. Then, with the object specified, you can then either specify a component of the GameObject to modify prefixed with ``!``, or a child of the GameObject to modify, for which this process recurses. 

To add a scene mod, simply create a JSON file within ``levels`` and give it the same name you would name a subfolder in ``textures``.
```
levels:
- FlowWormsSky.json
- RockPaperShowdown.json
```

#### Modifiable Components
The following components can currently be modified, with the following fields available to modify.
<!-- I am very discontent with whoever didn't include rowspan in markdown -->
<table>
  <tr><th>Component</th> <th>Field</th> <th>Field Type</th></tr>
  <tr><td rowspan="3"><code> !Transform </code></td> 
      <td><code> LocalPosition </code></td> <td> Vector3 </td></tr>
  <tr><td><code> LocalRotation </code></td> <td> Quaternion </td></tr>
  <tr><td><code> LocalScale </code></td>    <td> Vector3 </td></tr>
  
  <tr><td rowspan="4"><code> !SpriteRenderer </code></td> 
      <td><code> Color </code></td> <td> Color </td></tr>
  <tr><td><code> Size </code></td>  <td> Vector2 </td></tr>
  <tr><td><code> FlipX </code></td> <td> Boolean </td></tr>
  <tr><td><code> FlipY </code></td> <td> Boolean </td></tr>
  
  <tr><td rowspan="2"><code> !ParallaxObjectScript </code></td> 
      <td><code> ParallaxScale </code></td> <td> Float </td></tr>
  <tr><td><code> LoopDistance </code></td>  <td> Float </td></tr>
</table>

For fields with struct types like Vector3, Quaternion, and Color, you make the field's value a JSON object with fields for each member of the struct. 
 - Vectors have members ``x``, ``y``, and ``z`` for Vector3.
 - Quaternions have members ``x``, ``y``, ``z``, ``w``.
 - Color have members ``r``, ``g``, ``b``, ``a``.

For fields with primitive types like Boolean and Float, you make the field's value a JSON primitive.

#### Finding GameObject Names and Components
Short of ripping an entire Unity project from Bits & Bops, I recommend using the BepInEx plugin [RuntimeUnityEditor](https://github.com/ManlyMarco/RuntimeUnityEditor) to inspect the GameObject hierarchy of rhythm games.

## Configuration
After running Bits & Bops with the latest version of this plugin installed, a configuration file will be generated at `BepinEx\config\BopCustomTextures.cfg`. Open this file with a text editor to access the following configs:
| Name                         | Type              | Default       | Description   |
| ---------------------------- | ----------------- | ------------- | ------------- |
| `SaveCustomFiles`            | Boolean           | `true`        | <p>When opening a mixtape in the editor with custom files, save these files with the mixtape whenever the mixtape is saved.</p> |
| `LogFileLoading`             | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose file loading of custom files in .bop archives.</p> |
| `LogUnloading`               | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose custom asset unloading.</p> |
| `LogSeperateTextureSprites`  | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose custom sprite creation from seperate textures.</p> |
| `LogAtlasTextureSprites`     | BepInEx.LogLevel  | `Debug`       | <p>Log level for verbose custom sprite creation from atlas textures.</p> |
| `LogSceneIndices`            | BepInEx.LogLevel  | `None`        | <p>Log level for vanilla scene loading, including scene name + build index.</p> <p>Useful when you need to rip sprites from a sprite atlas, which requires knowing the build index of a game to locate its sharedassets file.</p> |


