using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PictureProcessingEditor : Editor
{
    [MenuItem("Assets/Image/二值化")]
    public static void Binarization()
    {
        var tex = Selection.activeObject as Texture2D;

        if (tex != null)
        {
            string path = AssetDatabase.GetAssetPath(tex);

            int index = path.LastIndexOf('.');

            path = path.Substring(0, index);

            var output = PictureProcessing.Binarization(tex);

            PictureProcessing.SaveImage(output, path);

            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Assets/Image/反色")]
    public static void InverseColor()
    {
        var tex = Selection.activeObject as Texture2D;

        if (tex != null)
        {
            string path = AssetDatabase.GetAssetPath(tex);

            int index = path.LastIndexOf('.');

            path = path.Substring(0, index);

            var output = PictureProcessing.InverseColor(tex);

            PictureProcessing.SaveImage(output, path);

            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Assets/Image/边")]
    public static void Edge()
    {
        var tex = Selection.activeObject as Texture2D;

        if (tex != null)
        {
            string path = AssetDatabase.GetAssetPath(tex);

            int index = path.LastIndexOf('.');

            path = path.Substring(0, index);

            var output = PictureProcessing.Edge(tex);

            PictureProcessing.SaveImage(output, path);

            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Assets/Image/灰度化")]
    public static void Grayscale()
    {
        var tex = Selection.activeObject as Texture2D;

        if (tex != null)
        {
            string path = AssetDatabase.GetAssetPath(tex);

            int index = path.LastIndexOf('.');

            path = path.Substring(0, index);

            var output = PictureProcessing.Grayscale(tex);

            PictureProcessing.SaveImage(output, path);

            AssetDatabase.Refresh();
        }
    }

    public static string GetObjectPath(Object assetObj)
    {
        string path = AssetDatabase.GetAssetPath(assetObj);

        int index = path.LastIndexOf('.');

        path = path.Substring(0, index);

        return path;
    }
}
