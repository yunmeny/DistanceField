using UnityEngine;

public class TestSDFGeneration : MonoBehaviour
{
    public Texture2D inputTexture;
    public Material displayMaterial;
    
    void Start()
    {
        if (inputTexture == null)
        {
            Debug.LogError("Input texture is null!");
            return;
        }
        
        // 生成 SDF
        Texture2D sdfTexture = DistanceField2D.GenerateSDF(inputTexture);
        
        // 检查生成的纹理格式
        Debug.Log("Generated SDF texture format: " + sdfTexture.format);
        Debug.Log("Generated SDF texture dimensions: " + sdfTexture.width + "x" + sdfTexture.height);
        
        // 显示生成的 SDF
        if (displayMaterial != null)
        {
            displayMaterial.mainTexture = sdfTexture;
        }
        
        // 保存 SDF 到文件
        byte[] exrData = sdfTexture.EncodeToEXR();
        System.IO.File.WriteAllBytes(Application.dataPath + "/../TestSDF.exr", exrData);
        Debug.Log("SDF saved to: " + Application.dataPath + "/../TestSDF.exr");
        
        // 验证一些像素值
        int centerX = sdfTexture.width / 2;
        int centerY = sdfTexture.height / 2;
        Color centerColor = sdfTexture.GetPixel(centerX, centerY);
        Debug.Log("Center pixel value: " + centerColor.r + " (should be around 0.5 if on edge)");
        
        // 检查单通道输出
        if (Mathf.Abs(centerColor.r - centerColor.g) < 0.001f && Mathf.Abs(centerColor.r - centerColor.b) < 0.001f)
        {
            Debug.Log("SDF is correctly single-channel (all RGB values are the same)");
        }
        else
        {
            Debug.LogWarning("SDF is not single-channel (RGB values differ)");
        }
    }
}