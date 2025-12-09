using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class DistanceField2DEditor : Editor
{
    [MenuItem("Assets/DistanceField/GenDF")]
    public static void CreateDFInside()
    {
        // 遍历所有选中的对象（支持多选）
        foreach (var selectedObj in Selection.objects)
        {
            // 过滤出Texture2D类型的对象
            var tex = selectedObj as Texture2D;
            if (tex == null)
            {
                Debug.LogWarning($"跳过非Texture2D对象：{selectedObj.name}");
                continue;
            }

            // 生成距离场
            var output = DistanceField2D.GenerateDF(tex, TextureFormat.RGBAFloat);
            if (output == null)
            {
                Debug.LogError($"生成DF失败：{tex.name}");
                continue;
            }

            // 处理保存路径（避免覆盖，兼容无扩展名的情况）
            string assetPath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"无法获取纹理路径：{tex.name}");
                continue;
            }

            int extIndex = assetPath.LastIndexOf('.');
            // 截取文件名（去掉扩展名）+ 后缀 + 新扩展名
            var outputPath = extIndex > 0 ? $"{assetPath[..extIndex]}_df.exr" : $"{assetPath}_df.exr";

            // 保存EXR文件
            byte[] exrData = output.EncodeToEXR();
            File.WriteAllBytes(outputPath, exrData);

            Debug.Log($"DF生成成功：{outputPath}");
        }

        // 刷新资源数据库，让Unity识别新生成的文件
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/DistanceField/GenSDF")]
    public static void CreateSDF()
    {
        // 遍历所有选中的对象（支持多选）
        foreach (var selectedObj in Selection.objects)
        {
            // 过滤出Texture2D类型的对象
            var tex = selectedObj as Texture2D;
            if (tex == null)
            {
                Debug.LogWarning($"跳过非Texture2D对象：{selectedObj.name}");
                continue;
            }

            // 生成有符号距离场（SDF）
            var output = DistanceField2D.GenerateSDF(tex, TextureFormat.RGFloat);
            if (output == null)
            {
                Debug.LogError($"生成SDF失败：{tex.name}");
                continue;
            }

            // 处理保存路径
            var assetPath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"无法获取纹理路径：{tex.name}");
                continue;
            }

            var extIndex = assetPath.LastIndexOf('.');
            var outputPath = extIndex > 0 ? $"{assetPath.Substring(0, extIndex)}_sdf.exr" : $"{assetPath}_sdf.exr";

            // 保存EXR文件
            var exrData = output.EncodeToEXR();
            File.WriteAllBytes(outputPath, exrData);

            Debug.Log($"SDF生成成功：{outputPath}");
        }

        // 刷新资源数据库
        AssetDatabase.Refresh();
    }
}