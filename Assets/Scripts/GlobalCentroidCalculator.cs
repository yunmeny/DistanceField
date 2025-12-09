using UnityEngine;
using UnityEngine.Serialization;

namespace DistanceField.Code
{
    [RequireComponent(typeof(MeshRenderer))]
    public class GlobalCentroidCalculator : MonoBehaviour
    {
        // Shader 属性ID
        private static readonly int TextureSample0 = Shader.PropertyToID("_Start");
        private static readonly int TextureSample1 = Shader.PropertyToID("_End");
        private static readonly int MovementCompensation = Shader.PropertyToID("_MovementCompensation");
        private static readonly int Progress = Shader.PropertyToID("_Progress");
        private static readonly int RotationCompensation = Shader.PropertyToID("_RotationCompensation");
        private static readonly int CurCenter = Shader.PropertyToID("_CurCenter");

        [SerializeField] [Range(0, 1)] private float progress; // 0~1之间的进度值
        
        // 自动进度相关参数
        [SerializeField] private bool autoProgress; // 自动进度开关
        [SerializeField] [Min(0.01f)] private float progressSpeed = 0.2f; // 自动进度速度（越大越快，最小0.01避免静止）
        
        [Tooltip("G通道的阈值：仅G值大于此阈值的像素会参与质心计算")] [Range(0f, 1f)]
        public float centroidThreshold = 0.2f;

        [SerializeField] private Vector2 movementCompensation; // 最终传给Shader的偏移补偿
        [SerializeField] private float rotationCompensation; // 最终传给Shader的旋转补偿
        [SerializeField] private Vector2 curCentroid; // Start纹理的质心（兼容原有逻辑）
        [SerializeField] private Vector2 targetCentroid; // End纹理的质心（兼容原有逻辑）

        [Header("Gizmos 绘制设置")]
        [SerializeField] private float gizmoSize = 0.05f; // 质心球体大小
        [SerializeField] private bool drawCompensationLine = true; // 是否绘制补偿向量线
        [SerializeField] private bool drawInterpolatedPoint = true; // 是否绘制进度插值点

        [Header("趋势线（主方向）设置")]
        [SerializeField] private float trendLineLength = 0.5f; // 趋势线绘制长度（世界空间）
        [SerializeField] private bool drawTrendLines = true; // 是否绘制质心趋势线
        [SerializeField] private bool drawInterpolatedTrendLine = true; // 是否绘制插值后的趋势线
        [SerializeField] private Color curTrendLineColor = new(1f, 0.2f, 0.2f); // 起始纹理趋势线颜色
        [SerializeField] private Color targetTrendLineColor = new(0.2f, 1f, 0.2f); // 目标纹理趋势线颜色
        [SerializeField] private Color interpolatedTrendLineColor = new(1f, 1f, 0.2f); // 插值趋势线颜色

        // 质心+主方向数据结构
        private struct CentroidAndDirection
        {
            public Vector2 centroid; // 质心（UV空间）
            public Vector2 mainDirection; // 主方向（单位向量，UV空间）
        }

        private CentroidAndDirection _curCentroidDir; // 起始纹理的质心+主方向
        private CentroidAndDirection _targetCentroidDir; // 目标纹理的质心+主方向

        private Material _material;
        private Renderer _renderer;
        private Texture2D _curTexture; // Start纹理（TextureSample0）
        private Texture2D _targetTexture; // End纹理（TextureSample1）
        
        private bool _prevAutoProgress; // 上一帧的自动开关状态
        private float _autoTimeOffset; // 时间偏移量，确保从当前progress开始

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            _prevAutoProgress = autoProgress; // 初始化状态
            InitMaterialAndTextures();
            CalculateAllCentroids();
            UpdateShaderCompensation();
        }

        void Update()
        {
            // 检测自动进度开关状态变化
            if (autoProgress != _prevAutoProgress)
            {
                _prevAutoProgress = autoProgress;
                
                if (autoProgress)
                {
                    // 开启自动模式时，计算时间偏移量，使初始值为当前progress
                    _autoTimeOffset = Time.time * progressSpeed - progress;
                }
            }

            // 自动进度逻辑：从当前progress开始，使用PingPong实现0-1往返
            if (autoProgress)
            {
                progress = Mathf.PingPong(Time.time * progressSpeed - _autoTimeOffset, 1f);
                UpdateShaderCompensation();
            }
        }
        
        void OnDrawGizmosSelected()
        {
            // 编辑模式下初始化必要组件
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }
            InitMaterialAndTextures();
            // 确保质心和方向已计算（编辑模式下手动触发）
            if (_curCentroidDir.centroid == Vector2.zero && _targetCentroidDir.centroid == Vector2.zero)
            {
                CalculateAllCentroids();
            }
            
            // 转换UV空间的质心到世界空间
            Vector3 curWorldPos = UVToWorldSpace(_curCentroidDir.centroid);
            Vector3 targetWorldPos = UVToWorldSpace(_targetCentroidDir.centroid);
            
            // 1. 绘制Start纹理质心（红色）
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(curWorldPos, gizmoSize);
            // 2. 绘制End纹理质心（绿色）
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetWorldPos, gizmoSize);
            // 3. 绘制补偿向量线（蓝色）
            if (drawCompensationLine)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(curWorldPos, targetWorldPos);
            }
            // 4. 绘制当前进度的插值质心点（黄色）
            if (drawInterpolatedPoint)
            {
                Vector3 interpolatedPos = Vector3.Lerp(curWorldPos, targetWorldPos, progress);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(interpolatedPos, gizmoSize * 0.8f); // 稍小一点区分
            }

            // 5. 绘制趋势线（主方向）
            if (drawTrendLines)
            {
                DrawTrendLine(curWorldPos, _curCentroidDir.mainDirection, curTrendLineColor);
                DrawTrendLine(targetWorldPos, _targetCentroidDir.mainDirection, targetTrendLineColor);
            }

            // 6. 绘制插值后的趋势线 - 使用旋转补偿角度插值
            if (drawInterpolatedTrendLine && drawInterpolatedPoint)
            {
                Vector3 interpolatedPos = Vector3.Lerp(curWorldPos, targetWorldPos, progress);
                // 使用旋转补偿角度进行方向插值，确保与计算逻辑一致
                float currentRotation = rotationCompensation * progress;
                Vector2 interpolatedDirUV = RotateVector2(_curCentroidDir.mainDirection, currentRotation);
                DrawTrendLine(interpolatedPos, interpolatedDirUV, interpolatedTrendLineColor, 0.8f);
            }
        }

        /// <summary>
        /// 绘制单条趋势线
        /// </summary>
        /// <param name="center">趋势线中心（世界空间）</param>
        /// <param name="uvDir">UV空间的方向向量</param>
        /// <param name="color">绘制颜色</param>
        /// <param name="lengthScale">长度缩放系数</param>
        private void DrawTrendLine(Vector3 center, Vector2 uvDir, Color color, float lengthScale = 1f)
        {
            Vector3 worldDir = UVDirectionToWorldSpace(uvDir);
            float finalLength = trendLineLength * lengthScale;
            Vector3 trendStart = center - worldDir * finalLength / 2;
            Vector3 trendEnd = center + worldDir * finalLength / 2;
            
            Gizmos.color = color;
            Gizmos.DrawLine(trendStart, trendEnd);
            
            // 绘制方向箭头（可选）
            Vector3 arrowTip = trendEnd;
            Vector3 arrowLeft = arrowTip - worldDir.normalized * finalLength * 0.1f + Quaternion.Euler(0, 0, 30) * (-worldDir).normalized * finalLength * 0.05f;
            Vector3 arrowRight = arrowTip - worldDir.normalized * finalLength * 0.1f + Quaternion.Euler(0, 0, -30) * (-worldDir).normalized * finalLength * 0.05f;
            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }
        
        /// <summary>
        /// UV空间坐标转世界空间坐标
        /// </summary>
        private Vector3 UVToWorldSpace(Vector2 uv)
        {
            Vector3 localPos = new Vector3((uv.x - 0.5f)*8, (uv.y - 0.5f)*8, 0);
            return localPos;
        }

        /// <summary>
        /// UV空间方向向量转世界空间方向向量（仅缩放，不偏移）
        /// </summary>
        private Vector3 UVDirectionToWorldSpace(Vector2 uvDir)
        {
            return new Vector3(uvDir.x * 8, uvDir.y * 8, 0);
        }
        
        private void InitMaterialAndTextures()
        {
            if (_material == null && _renderer != null)
            {
                _material = _renderer.sharedMaterial;
            }

            if (_material != null)
            {
                _curTexture = _material.GetTexture(TextureSample0) as Texture2D;
                _targetTexture = _material.GetTexture(TextureSample1) as Texture2D;

                if (_curTexture == null)
                {
                    Debug.LogWarning("材质中Start纹理不是Texture2D类型或未赋值！", this);
                }

                if (_targetTexture == null)
                {
                    Debug.LogWarning("材质中_End纹理不是Texture2D类型或未赋值！", this);
                }
            }
        }
        
        [ContextMenu("重新计算所有质心和趋势线")]
        private void CalculateAllCentroids()
        {
            _curCentroidDir = CalculateCentroidAndDirection(_curTexture, centroidThreshold);
            _targetCentroidDir = CalculateCentroidAndDirection(_targetTexture, centroidThreshold);
            
            // 保留原有变量兼容
            curCentroid = _curCentroidDir.centroid;
            targetCentroid = _targetCentroidDir.centroid;
            movementCompensation = targetCentroid - curCentroid;
            
            // 修改部分：计算最快到达目标方向的角度（不考虑方向的正反）
            rotationCompensation = CalculateShortestRotationAngle(_curCentroidDir.mainDirection, _targetCentroidDir.mainDirection);
        }
        
        /// <summary>
        /// 计算两个方向之间的最短旋转角度（不考虑方向的正反，只要平行即可）
        /// </summary>
        /// <param name="fromDir">起始方向</param>
        /// <param name="toDir">目标方向</param>
        /// <returns>最短旋转角度（弧度），范围在[-π/2, π/2]</returns>
        private float CalculateShortestRotationAngle(Vector2 fromDir, Vector2 toDir)
        {
            // 计算点积
            float dot = Vector2.Dot(fromDir, toDir);
            
            // 计算叉积（z分量）
            float cross = fromDir.x * toDir.y - fromDir.y * toDir.x;
            
            // 使用Atan2计算基础角度
            float baseAngle = Mathf.Atan2(cross, dot);
            
            // 归一化到[-π, π]范围
            if (baseAngle > Mathf.PI) baseAngle -= 2 * Mathf.PI;
            if (baseAngle < -Mathf.PI) baseAngle += 2 * Mathf.PI;
            
            // 关键修改：如果需要旋转超过90度，就旋转180-角度，因为方向相反也算平行
            if (baseAngle > Mathf.PI / 2)
            {
                baseAngle = baseAngle - Mathf.PI;
            }
            else if (baseAngle < -Mathf.PI / 2)
            {
                baseAngle = baseAngle + Mathf.PI;
            }
            
            // 确保角度在[-π/2, π/2]范围内
            return Mathf.Clamp(baseAngle, -Mathf.PI / 2, Mathf.PI / 2);
        }
        
        /// <summary>
        /// 计算纹理的质心和主方向（趋势线）
        /// </summary>
        /// <param name="texture">目标纹理</param>
        /// <param name="threshold">G通道阈值</param>
        /// <returns>质心+主方向数据</returns>
        private CentroidAndDirection CalculateCentroidAndDirection(Texture2D texture, float threshold)
        {
            CentroidAndDirection result = new()
            {
                centroid = new Vector2(0.5f, 0.5f), // 默认中心
                mainDirection = Vector2.right // 默认方向（右）
            };

            if (texture == null)
            {
                Debug.LogWarning("计算质心和方向时纹理为空，返回默认值", this);
                return result;
            }

            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;

            // ========== 第一步：计算质心 ==========
            Vector2 centroidAccum = Vector2.zero;
            float totalWeight = 0f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color col = pixels[y * width + x];
                    float weight = Mathf.Max(0f, col.g - threshold); // 仅G通道大于阈值的像素参与计算

                    if (weight > 0f)
                    {
                        Vector2 uv = new Vector2((float)x / width, (float)y / height);
                        centroidAccum += uv * weight;
                        totalWeight += weight;
                    }
                }
            }

            // 无有效像素时返回默认值
            if (totalWeight <= Mathf.Epsilon)
            {
                return result;
            }

            // 归一化得到质心
            result.centroid = centroidAccum / totalWeight;

            // ========== 第二步：计算二阶矩（协方差矩阵），求解主方向 ==========
            float sumXX = 0f; // 相对于质心的x偏移平方和
            float sumYY = 0f; // 相对于质心的y偏移平方和
            float sumXY = 0f; // 相对于质心的x/y偏移乘积和

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color col = pixels[y * width + x];
                    float weight = Mathf.Max(0f, col.g - threshold);

                    if (weight > 0f)
                    {
                        Vector2 uv = new Vector2((float)x / width, (float)y / height);
                        Vector2 delta = uv - result.centroid; // 像素相对于质心的偏移

                        sumXX += delta.x * delta.x * weight;
                        sumYY += delta.y * delta.y * weight;
                        sumXY += delta.x * delta.y * weight;
                    }
                }
            }

            // 归一化协方差矩阵（除以总权重）
            sumXX /= totalWeight;
            sumYY /= totalWeight;
            sumXY /= totalWeight;

            // ========== 求解2x2协方差矩阵的特征值和特征向量 ==========
            // 协方差矩阵：[sumXX, sumXY; sumXY, sumYY]
            // 特征方程：λ² - (sumXX+sumYY)λ + (sumXX*sumYY - sumXY²) = 0
            float trace = sumXX + sumYY; // 矩阵迹
            float det = sumXX * sumYY - sumXY * sumXY; // 矩阵行列式

            // 特征值计算（避免开方负数）
            float discriminant = (trace * trace / 4) - det;
            discriminant = Mathf.Max(discriminant, 0f); // 数值稳定性修正
            float sqrtDisc = Mathf.Sqrt(discriminant);
            
            float lambda1 = trace / 2 + sqrtDisc; // 最大特征值（主方向对应的特征值）
            float lambda2 = trace / 2 - sqrtDisc; // 最小特征值

            // 计算最大特征值对应的特征向量（主方向）
            if (Mathf.Abs(sumXY) < Mathf.Epsilon)
            {
                // 对角矩阵：方向沿X或Y轴
                result.mainDirection = lambda1 > lambda2 ? Vector2.right : Vector2.up;
            }
            else
            {
                // 非对角矩阵：求解特征向量
                // 特征向量满足：(sumXX - λ)x + sumXY * y = 0
                float x = 1f;
                float y = (lambda1 - sumXX) * x / sumXY;
                result.mainDirection = new Vector2(x, y).normalized; // 归一化为单位向量
            }

            return result;
        }
        
        /// <summary>
        /// 旋转二维向量
        /// </summary>
        private Vector2 RotateVector2(Vector2 v, float angle)
        {
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
        
        private void UpdateShaderCompensation()
        {
            if (_material)
            {
                _material.SetVector(MovementCompensation, movementCompensation);
                _material.SetFloat(Progress, progress);
                _material.SetFloat(RotationCompensation, rotationCompensation);
                _material.SetVector(CurCenter,curCentroid);
            }
        }
        
        void OnValidate()
        {
            _renderer = GetComponent<Renderer>();
            InitMaterialAndTextures();
            CalculateAllCentroids();
            UpdateShaderCompensation();
        }
    }
}