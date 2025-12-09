using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace DistanceField.Code
{
    /// <summary>
    /// 纹理图集生成器，用于预渲染动画序列到纹理图集
    /// </summary>
    public class TextureAtlasGenerator : MonoBehaviour
    {
        [Header("动画参数")] [SerializeField] [Range(1, 128)]
        private int frameCount = 16; // 总帧数

        [SerializeField] [Range(64, 2048)] private int frameResolution = 128; // 单帧分辨率
        [SerializeField] [Range(0.1f, 10.0f)] private float animationDuration = 2.0f; // 动画时长（秒）

        [Header("图集参数")] [SerializeField] private bool autoCalculateLayout = true; // 自动计算图集布局
        [SerializeField] [Range(1, 32)] private int rows = 4; // 行数
        [SerializeField] [Range(1, 32)] private int cols = 4; // 列数

        [Header("输出设置")] [SerializeField] private string atlasName = "AnimationAtlas"; // 图集名称
        [SerializeField] private TextureFormat textureFormat = TextureFormat.RGBA32; // 纹理格式
        [SerializeField] private bool generateMipMaps = false; // 是否生成MipMaps
        [SerializeField] private bool enableCompression = true; // 是否启用压缩

        [Header("相机")] [SerializeField] private Camera renderCamera; // 动画相机

        [Header("引用")] [SerializeField] private RenderTexture renderTexture; // 渲染目标

        Material[] targetMaterials; // 目标材质

        // 材质属性ID
        private static readonly int Progress = Shader.PropertyToID("_Progress");

        /// <summary>
        /// 生成纹理图集
        /// </summary>
        public void GenerateTextureAtlas()
        {
            // 验证参数
            if (!ValidateParameters())
            {
                Debug.LogError("参数验证失败，无法生成纹理图集");
                return;
            }

            // 计算图集布局
            CalculateAtlasLayout();

            // 创建临时渲染纹理
            var tempRT = CreateTemporaryRenderTexture();
            if (tempRT == null)
            {
                Debug.LogError("无法创建临时渲染纹理");
                return;
            }

            // 创建输出纹理图集
            Texture2D atlasTexture = CreateAtlasTexture();

            // 渲染每一帧
            RenderFrames(tempRT, atlasTexture);

            // 保存纹理图集
            SaveTextureAtlas(atlasTexture);

            // 清理资源
            Cleanup(tempRT);

            Debug.Log("纹理图集生成完成：" + atlasName);
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        private bool ValidateParameters()
        {
            if (!renderCamera)
            {
                Debug.LogError("缺少RenderCamera引用");
            }

            targetMaterials = transform.GetComponentsInChildren<MeshRenderer>().Select(r => r.sharedMaterial).ToArray();
            if (targetMaterials.Length == 0)
            {
                Debug.LogError("缺少TargetMaterial引用");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 计算图集布局
        /// </summary>
        private void CalculateAtlasLayout()
        {
            if (autoCalculateLayout)
            {
                // 计算最小行数和列数
                cols = Mathf.CeilToInt(Mathf.Sqrt(frameCount));
                rows = Mathf.CeilToInt((float)frameCount / cols);
            }

            // 确保行数和列数足够容纳所有帧
            int requiredCells = rows * cols;
            if (requiredCells < frameCount)
            {
                Debug.LogWarning("图集布局不足以容纳所有帧，自动调整");
                cols = Mathf.CeilToInt(Mathf.Sqrt(frameCount));
                rows = Mathf.CeilToInt((float)frameCount / cols);
            }
        }

        /// <summary>
        /// 创建临时渲染纹理
        /// </summary>
        private RenderTexture CreateTemporaryRenderTexture()
        {
            if (renderTexture)
            {
                return renderTexture;
            }

            // 创建临时渲染纹理
            var tempRT = new RenderTexture(frameResolution, frameResolution, 24, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true
            };
            tempRT.Create();

            return tempRT;
        }

        /// <summary>
        /// 创建图集纹理
        /// </summary>
        private Texture2D CreateAtlasTexture()
        {
            int atlasWidth = cols * frameResolution;
            int atlasHeight = rows * frameResolution;

            Texture2D atlasTexture = new Texture2D(atlasWidth, atlasHeight, textureFormat, generateMipMaps);
            atlasTexture.filterMode = FilterMode.Bilinear;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;

            return atlasTexture;
        }


        // 创建一个专用的渲染层
        [SerializeField] private int targetLayer = 31; // 使用第31层（默认未使用）

        /// <summary>
        /// 渲染每一帧
        /// </summary>
        private void RenderFrames(RenderTexture tempRT, Texture2D atlasTexture)
        {
            // 保存当前渲染目标
            var previousRT = RenderTexture.active;
            var all = transform.GetComponentsInChildren<MeshRenderer>().Select(m => m.gameObject).ToArray();
            var previousLayers = new int[all.Length];
            try
            {
                // 设置相机只渲染目标层
                renderCamera.cullingMask = 1 << targetLayer; // 只渲染目标层
                renderCamera.clearFlags = CameraClearFlags.SolidColor; // 清除标志
                renderCamera.backgroundColor = Color.clear; // 透明背景
                renderCamera.targetTexture = tempRT; // 设置目标纹理
                
                // 遍历所有MeshRenderer并设置为目标层
                for (int i = 0; i < all.Length; i++)
                {
                    previousLayers[i] = all[i].gameObject.layer;
                    all[i].gameObject.layer = targetLayer;
                }
                
                // 渲染每一帧
                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    // 计算当前进度
                    var progress = (float)frameIndex / (frameCount - 1);

                    // 设置材质属性
                    foreach (var material in targetMaterials)
                    {
                        material.SetFloat(Progress, progress);
                    }

                    // 渲染到临时纹理
                    RenderTexture.active = tempRT;
                    renderCamera.targetTexture = tempRT;
                    renderCamera.Render();

                    // 读取像素数据
                    var frameTexture = new Texture2D(frameResolution, frameResolution, textureFormat, generateMipMaps);
                    frameTexture.ReadPixels(new Rect(0, 0, frameResolution, frameResolution), 0, 0);
                    frameTexture.Apply();

                    // 将帧纹理复制到图集
                    CopyFrameToAtlas(frameTexture, atlasTexture, frameIndex);

                    // 释放帧纹理
                    DestroyImmediate(frameTexture);

                    // 更新进度
                    EditorUtility.DisplayProgressBar("生成纹理图集", $"渲染帧 {frameIndex + 1}/{frameCount}",
                        (float)frameIndex / frameCount);
                }
            }
            finally
            {
                
                // 恢复原始渲染层
                for (int i = 0; i < all.Length; i++)
                {
                    all[i].gameObject.layer = previousLayers[i];
                }
                
                // 恢复原始渲染目标
                RenderTexture.active = previousRT;
                renderCamera.targetTexture = null;

                // 清除进度条
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 将单帧复制到图集
        /// </summary>
        private void CopyFrameToAtlas(Texture2D frameTexture, Texture2D atlasTexture, int frameIndex)
        {
            // 计算当前帧在图集中的位置
            var col = frameIndex % cols;
            var row = frameIndex / cols;

            var xOffset = col * frameResolution;
            var yOffset = row * frameResolution;

            // 获取帧纹理像素
            var framePixels = frameTexture.GetPixels();

            // 将像素复制到图集
            atlasTexture.SetPixels(xOffset, yOffset, frameResolution, frameResolution, framePixels);
        }

        /// <summary>
        /// 保存纹理图集
        /// </summary>
        private void SaveTextureAtlas(Texture2D atlasTexture)
        {
            // 应用纹理修改
            atlasTexture.Apply();

            // 设置压缩格式
            if (enableCompression)
            {
                TextureImporterPlatformSettings platformSettings = new TextureImporterPlatformSettings();
                platformSettings.overridden = true;
                platformSettings.format = TextureImporterFormat.Automatic;
            }

            // 保存到文件
            string savePath = EditorUtility.SaveFilePanel("保存纹理图集", "Assets", atlasName, "png");
            if (!string.IsNullOrEmpty(savePath))
            {
                // 转换为相对路径
                string relativePath = savePath.Replace(Application.dataPath, "Assets");

                // 保存纹理
                byte[] pngData = atlasTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(savePath, pngData);

                // 导入到Unity
                AssetDatabase.ImportAsset(relativePath);

                // 设置纹理导入设置
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = generateMipMaps;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;

                    if (enableCompression)
                    {
                        importer.textureCompression = TextureImporterCompression.Compressed;
                    }
                    else
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                    }

                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup(RenderTexture tempRT)
        {
            if (tempRT != null && tempRT != renderTexture)
            {
                tempRT.Release();
                DestroyImmediate(tempRT);
            }
        }

        /// <summary>
        /// 在Inspector中显示生成按钮
        /// </summary>
        [CustomEditor(typeof(TextureAtlasGenerator))]
        private class TextureAtlasGeneratorEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                TextureAtlasGenerator generator = (TextureAtlasGenerator)target;

                // 添加生成按钮
                if (GUILayout.Button("生成纹理图集"))
                {
                    generator.GenerateTextureAtlas();
                }
            }
        }
    }
}