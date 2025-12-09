using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceField2D
{
    private static readonly int TextureR = Shader.PropertyToID("_TextureR");
    private static readonly int TextureG = Shader.PropertyToID("_TextureG");
    private static readonly int Texture = Shader.PropertyToID("_Texture");
    private static readonly int Type = Shader.PropertyToID("_Type");
    private static readonly int SDFTexture = Shader.PropertyToID("_SDFTexture");
    private static readonly int Level = Shader.PropertyToID("_Level");
    private static readonly int Inverse = Shader.PropertyToID("_Inverse");

    public static Texture2D GenerateDF(Texture texture, TextureFormat format = TextureFormat.RGBA32, bool Inverse = false)
    {
        var iterNum = 15;
        var shader = Shader.Find("SDF/JFA");
        var material = new Material(shader);
        material.SetTexture(Texture, texture);
        var rt = CreateRT(texture.width, texture.height);
        var tempRT = CreateRT(texture.width, texture.height);
        material.SetInt(Type, 0);
        material.SetInt(DistanceField2D.Inverse, Inverse ? 1 : 0);
        for (var i = 0; i < iterNum; i++)
        {
            material.SetInt(Level, i);
            material.SetTexture(SDFTexture, tempRT);
            Graphics.Blit(tempRT, rt, material);
            (tempRT, rt) = (rt, tempRT);
        }
        material.SetInt(Type, 1);
        material.SetTexture(Texture, tempRT);
        Graphics.Blit(null, rt, material);
        var output = TextureUtil.ToTexture(rt, format);
        rt.Release();
        tempRT.Release();
        return output;
    }

    public static Texture2D GenerateSDF(Texture texture, TextureFormat format = TextureFormat.RGFloat)
    {
        var tex1 = GenerateDF(texture, format, false);
        var tex2 = GenerateDF(texture, format, true);
        var shader = Shader.Find("Image/ChannelMerge");
        var material = new Material(shader);
        var rt = CreateRT(texture.width, texture.height);
        material.SetTexture(TextureR, tex1);
        material.SetTexture(TextureG, tex2);
        Graphics.Blit(null, rt, material);
        return TextureUtil.ToTexture(rt,TextureFormat.RGBAFloat);
    }


    private static RenderTexture CreateRT(int width,int height)
    {
        var rt = new RenderTexture(width, height, 0)
        {
            format = RenderTextureFormat.ARGBFloat,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        return rt;
    }


    public static void ToTex2DAndSave(RenderTexture rt, string fileName, TextureFormat format = TextureFormat.RGBA32)
    {
        Texture2D output = new Texture2D(rt.width, rt.height, format, false);

        RenderTexture currentActiveRT = RenderTexture.active;

        RenderTexture.active = rt;

        output.ReadPixels(new Rect(0, 0, output.width, output.height), 0, 0);

        output.Apply();

        RenderTexture.active = currentActiveRT;

        byte[] data = output.EncodeToPNG();

        System.IO.File.WriteAllBytes(fileName + ".png", data);

        //AssetDatabase.Refresh();
    }
}
