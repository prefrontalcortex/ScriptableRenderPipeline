using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MakeTextureArray : ScriptableObject
{
    public List<Texture2D> tex;
#if UNITY_EDITOR
    [ContextMenu("Make Array")]
    void MakeArray()
    {
        var t = new Texture2DArray(tex[0].width, tex[0].height, tex.Count, tex[0].format, true);
        for(int i = 0; i < tex.Count; i++)
        {
            Graphics.CopyTexture(tex[i], 0, t, i);
        }

        UnityEditor.AssetDatabase.CreateAsset(t, "Assets/Scenes/004_xr_ShaderGraph_Arrays/array.asset");
    }
#endif
}
