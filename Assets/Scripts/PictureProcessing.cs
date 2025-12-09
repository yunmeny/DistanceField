using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PictureProcessing
{

    public static RenderTexture CreateRT(int width,int height)
    {
        RenderTexture rt = new RenderTexture(width,height, 0);

        rt.format = RenderTextureFormat.ARGBFloat;

        rt.Create();

        return rt;
    }
    
    public static Texture2D Binarization(Texture2D tex)
    {
        var m_material = new Material(Shader.Find("Image/Binarization"));

        m_material.SetTexture("_MainTex", tex);

        RenderTexture rt = CreateRT(tex.width, tex.height);

        Graphics.Blit(null, rt, m_material); ;

        return TextureUtil.ToTexture(rt);
    }

    /// <summary>
    /// ��ɫ
    /// </summary>
    /// <param name="tex">ԭʼͼ��</param>
    /// <returns></returns>
    public static Texture2D InverseColor(Texture2D tex)
    {
        var m_material = new Material(Shader.Find("Image/InverseColor"));

        m_material.SetTexture("_MainTex", tex);

        RenderTexture rt = CreateRT(tex.width, tex.height);

        Graphics.Blit(null, rt, m_material); ;

        return TextureUtil.ToTexture(rt);
    }
    /// <summary>
    /// ��ȡ��
    /// </summary>
    /// <param name="tex"></param>
    /// <returns></returns>
    public static Texture2D Edge(Texture2D tex)
    {
        var m_material = new Material(Shader.Find("Image/Edge"));

        m_material.SetTexture("_MainTex", tex);
        m_material.SetVector("_TexelSize", new Vector4(1.0f/tex.width, 1.0f/tex.height,tex.width,tex.height));

        RenderTexture rt = CreateRT(tex.width, tex.height);

        Graphics.Blit(null, rt, m_material); ;

        return TextureUtil.ToTexture(rt);
    }

    public static Texture2D Grayscale(Texture2D tex)
    {
        var m_material = new Material(Shader.Find("Image/Grayscale"));

        m_material.SetTexture("_MainTex", tex);

        RenderTexture rt = CreateRT(tex.width, tex.height);

        Graphics.Blit(null, rt, m_material); ;

        return TextureUtil.ToTexture(rt);
    }


    public static void SaveImage(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();

        System.IO.File.WriteAllBytes(path+ "_copy.png", bytes);
    }

    public static void Save(RenderTexture rt, string fileName, TextureFormat format = TextureFormat.RGBA32)
    {
        var output = TextureUtil.ToTexture(rt, format);

        byte[] data = output.EncodeToPNG();

        System.IO.File.WriteAllBytes(fileName + "_copy.png", data);
    }
}
