using UnityEngine;

namespace BopCustomTextures.SceneMods;
public interface IMRenderable
{
    MMaterial MMaterial { get; set; }
    Material Material { get; set; }
}
