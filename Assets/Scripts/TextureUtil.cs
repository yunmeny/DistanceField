using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureUtil
{
    public static Texture2D ToTexture(RenderTexture rt, TextureFormat format = TextureFormat.ARGB32)
    {
        Texture2D output = new Texture2D(rt.width, rt.height, format, false);

        RenderTexture currentActiveRT = RenderTexture.active;

        RenderTexture.active = rt;

        output.ReadPixels(new Rect(0, 0, output.width, output.height), 0, 0);

        output.Apply();

        RenderTexture.active = currentActiveRT;

        return output;
    }
}
