using System;
using UnityEngine;

namespace DistanceField.Code
{
    /// <summary>
    /// 帧动画播放模式
    /// </summary>
    public enum AnimationPlayMode
    {
        Once,           // 播放一次
        Loop,           // 循环播放
        PingPong,       // 往返播放
        Random          // 随机播放
    }
    
    /// <summary>
    /// 帧动画播放器，用于控制帧动画的播放
    /// </summary>
    public class FrameAnimationPlayer : MonoBehaviour
    {
        [Header("动画参数")]
        [SerializeField] private AnimationPlayMode playMode = AnimationPlayMode.Loop; // 播放模式
        [SerializeField] private float animationSpeed = 1.0f; // 播放速度
        [SerializeField] private bool playOnAwake = false; // 唤醒时自动播放
        [SerializeField] private bool isPaused = false; // 是否暂停
        [SerializeField] private int startFrame = 0; // 开始帧
        
        [Header("图集参数")]
        [SerializeField] private Texture2D frameAtlas; // 帧动画图集
        [SerializeField] [Range(1, 128)] private int frameCount = 16; // 总帧数
        [SerializeField] [Range(1, 32)] private int rows = 4; // 行数
        [SerializeField] [Range(1, 32)] private int cols = 4; // 列数
        [SerializeField] [Range(0.0f, 1.0f)] private float frameSmooth = 0.1f; // 帧平滑过渡
        
        [Header("引用")]
        [SerializeField] private Material targetMaterial; // 目标材质
        [SerializeField] private MeshRenderer[] meshRenderers; // 目标渲染器数组
        
        // 材质属性ID
        private static readonly int FrameAtlas = Shader.PropertyToID("_FrameAtlas");
        private static readonly int FrameCount = Shader.PropertyToID("_FrameCount");
        private static readonly int CurrentFrame = Shader.PropertyToID("_CurrentFrame");
        private static readonly int FrameRows = Shader.PropertyToID("_FrameRows");
        private static readonly int FrameCols = Shader.PropertyToID("_FrameCols");
        private static readonly int FrameSmooth = Shader.PropertyToID("_FrameSmooth");
        private static readonly int UseFrameAnimation = Shader.PropertyToID("_UseFrameAnimation");
        
        // 内部状态
        private float _currentTime = 0.0f; // 当前时间
        private float _frameDuration; // 单帧时长
        private bool _isPlayingForward = true; // 是否正向播放
        private int _previousFrame = -1; // 上一帧索引
        private MaterialPropertyBlock _propertyBlock; // 材质属性块，用于优化性能
        
        /// <summary>
        /// 当前播放的帧索引
        /// </summary>
        public float CurrentFrameIndex { get; private set; } = 0.0f;
        
        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => !isPaused;
        
        /// <summary>
        /// 动画是否已完成（仅适用于Once模式）
        /// </summary>
        public bool IsCompleted { get; private set; } = false;
        
        private void Awake()
        {
            // 初始化属性块
            _propertyBlock = new MaterialPropertyBlock();
            
            // 计算单帧时长
            CalculateFrameDuration();
            
            // 初始化材质属性
            InitializeMaterialProperties();
            
            // 设置初始帧
            SetCurrentFrame(startFrame);
            
            // 唤醒时自动播放
            if (playOnAwake)
            {
                Play();
            }
        }
        
        private void Update()
        {
            if (!isPaused && !IsCompleted)
            {
                UpdateAnimation();
            }
        }
        
        /// <summary>
        /// 计算单帧时长
        /// </summary>
        private void CalculateFrameDuration()
        {
            _frameDuration = 1.0f / (frameCount * animationSpeed);
        }
        
        /// <summary>
        /// 初始化材质属性
        /// </summary>
        private void InitializeMaterialProperties()
        {
            // 设置所有目标材质的属性
            SetMaterialProperty(FrameAtlas, frameAtlas);
            SetMaterialProperty(FrameCount, frameCount);
            SetMaterialProperty(FrameRows, rows);
            SetMaterialProperty(FrameCols, cols);
            SetMaterialProperty(FrameSmooth, frameSmooth);
            SetMaterialProperty(UseFrameAnimation, 1); // 启用帧动画
        }
        
        /// <summary>
        /// 更新动画
        /// </summary>
        private void UpdateAnimation()
        {
            // 更新时间
            _currentTime += Time.deltaTime * animationSpeed;
            
            // 根据播放模式更新当前帧
            UpdateCurrentFrame();
            
            // 更新材质属性
            UpdateMaterialProperties();
        }
        
        /// <summary>
        /// 根据播放模式更新当前帧
        /// </summary>
        private void UpdateCurrentFrame()
        {
            float totalDuration = frameCount * _frameDuration;
            
            switch (playMode)
            {
                case AnimationPlayMode.Once:
                    // 播放一次
                    _currentTime = Mathf.Clamp(_currentTime, 0, totalDuration);
                    CurrentFrameIndex = Mathf.Clamp01(_currentTime / totalDuration) * (frameCount - 1);
                    IsCompleted = _currentTime >= totalDuration;
                    break;
                    
                case AnimationPlayMode.Loop:
                    // 循环播放
                    float loopTime = _currentTime % totalDuration;
                    CurrentFrameIndex = (loopTime / totalDuration) * (frameCount - 1);
                    break;
                    
                case AnimationPlayMode.PingPong:
                    // 往返播放
                    float pingPongTime = _currentTime % (totalDuration * 2);
                    if (pingPongTime < totalDuration)
                    {
                        // 正向播放
                        _isPlayingForward = true;
                        CurrentFrameIndex = (pingPongTime / totalDuration) * (frameCount - 1);
                    }
                    else
                    {
                        // 反向播放
                        _isPlayingForward = false;
                        float reverseTime = pingPongTime - totalDuration;
                        CurrentFrameIndex = (1.0f - reverseTime / totalDuration) * (frameCount - 1);
                    }
                    break;
                    
                case AnimationPlayMode.Random:
                    // 随机播放
                    int randomFrame = Mathf.FloorToInt(_currentTime / _frameDuration) % frameCount;
                    CurrentFrameIndex = randomFrame;
                    break;
            }
        }
        
        /// <summary>
        /// 更新材质属性
        /// </summary>
        private void UpdateMaterialProperties()
        {
            // 只在帧变化时更新材质属性
            int currentFrameInt = Mathf.FloorToInt(CurrentFrameIndex);
            if (currentFrameInt != _previousFrame)
            {
                SetMaterialProperty(CurrentFrame, CurrentFrameIndex);
                _previousFrame = currentFrameInt;
            }
        }
        
        /// <summary>
        /// 设置材质属性
        /// </summary>
        /// <param name="propertyId">属性ID</param>
        /// <param name="value">属性值</param>
        private void SetMaterialProperty(int propertyId, float value)
        {
            if (targetMaterial != null)
            {
                targetMaterial.SetFloat(propertyId, value);
            }
            
            foreach (var renderer in meshRenderers)
            {
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetFloat(propertyId, value);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }
        
        /// <summary>
        /// 设置材质属性
        /// </summary>
        /// <param name="propertyId">属性ID</param>
        /// <param name="value">属性值</param>
        private void SetMaterialProperty(int propertyId, int value)
        {
            if (targetMaterial != null)
            {
                targetMaterial.SetInt(propertyId, value);
            }
            
            foreach (var renderer in meshRenderers)
            {
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetInt(propertyId, value);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }
        
        /// <summary>
        /// 设置材质属性
        /// </summary>
        /// <param name="propertyId">属性ID</param>
        /// <param name="value">属性值</param>
        private void SetMaterialProperty(int propertyId, Texture value)
        {
            if (targetMaterial != null)
            {
                targetMaterial.SetTexture(propertyId, value);
            }
            
            foreach (var renderer in meshRenderers)
            {
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetTexture(propertyId, value);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        public void Play()
        {
            isPaused = false;
            IsCompleted = false;
        }
        
        /// <summary>
        /// 暂停动画
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }
        
        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            isPaused = true;
            IsCompleted = true;
            ResetToStart();
        }
        
        /// <summary>
        /// 重置动画到起始帧
        /// </summary>
        public void ResetToStart()
        {
            _currentTime = 0.0f;
            SetCurrentFrame(startFrame);
            IsCompleted = false;
        }
        
        /// <summary>
        /// 设置当前帧
        /// </summary>
        /// <param name="frameIndex">帧索引</param>
        public void SetCurrentFrame(int frameIndex)
        {
            // 限制帧索引范围
            frameIndex = Mathf.Clamp(frameIndex, 0, frameCount - 1);
            
            CurrentFrameIndex = frameIndex;
            _previousFrame = -1; // 强制更新材质
            
            // 更新时间
            _currentTime = frameIndex * _frameDuration;
            
            // 更新材质属性
            UpdateMaterialProperties();
        }
        
        /// <summary>
        /// 设置播放速度
        /// </summary>
        /// <param name="speed">播放速度</param>
        public void SetSpeed(float speed)
        {
            animationSpeed = Mathf.Max(0.01f, speed);
            CalculateFrameDuration();
        }
        
        /// <summary>
        /// 设置播放模式
        /// </summary>
        /// <param name="mode">播放模式</param>
        public void SetPlayMode(AnimationPlayMode mode)
        {
            playMode = mode;
            IsCompleted = false;
        }
        
        /// <summary>
        /// 设置帧动画图集
        /// </summary>
        /// <param name="atlas">纹理图集</param>
        /// <param name="newFrameCount">帧数</param>
        /// <param name="newRows">行数</param>
        /// <param name="newCols">列数</param>
        public void SetFrameAtlas(Texture2D atlas, int newFrameCount, int newRows, int newCols)
        {
            frameAtlas = atlas;
            frameCount = newFrameCount;
            rows = newRows;
            cols = newCols;
            
            CalculateFrameDuration();
            InitializeMaterialProperties();
        }
        
        /// <summary>
        /// 切换播放/暂停状态
        /// </summary>
        public void TogglePlayPause()
        {
            if (isPaused)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }
        
        /// <summary>
        /// 前进一帧
        /// </summary>
        public void NextFrame()
        {
            int nextFrame = Mathf.FloorToInt(CurrentFrameIndex) + 1;
            if (nextFrame >= frameCount)
            {
                nextFrame = 0;
            }
            SetCurrentFrame(nextFrame);
        }
        
        /// <summary>
        /// 后退一帧
        /// </summary>
        public void PreviousFrame()
        {
            int prevFrame = Mathf.FloorToInt(CurrentFrameIndex) - 1;
            if (prevFrame < 0)
            {
                prevFrame = frameCount - 1;
            }
            SetCurrentFrame(prevFrame);
        }
        
        /// <summary>
        /// 添加目标渲染器
        /// </summary>
        /// <param name="renderer">渲染器</param>
        public void AddMeshRenderer(MeshRenderer renderer)
        {
            if (renderer != null && !Array.Exists(meshRenderers, r => r == renderer))
            {
                Array.Resize(ref meshRenderers, meshRenderers.Length + 1);
                meshRenderers[meshRenderers.Length - 1] = renderer;
                
                // 初始化新渲染器的材质属性
                InitializeMaterialProperties();
            }
        }
        
        /// <summary>
        /// 移除目标渲染器
        /// </summary>
        /// <param name="renderer">渲染器</param>
        public void RemoveMeshRenderer(MeshRenderer renderer)
        {
            if (renderer != null)
            {
                meshRenderers = Array.FindAll(meshRenderers, r => r != renderer);
            }
        }
        
        /// <summary>
        /// 清空目标渲染器
        /// </summary>
        public void ClearMeshRenderers()
        {
            meshRenderers = new MeshRenderer[0];
        }
    }
}
