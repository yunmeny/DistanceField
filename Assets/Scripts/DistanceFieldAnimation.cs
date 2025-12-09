using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DistanceFieldAnimation : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("动画播放速度")]
    public float animationSpeed = 0.5f;
    
    [Tooltip("是否循环播放")]
    public bool loopAnimation = true;
    
    [Tooltip("是否反向播放")]
    public bool reverse = false;
    
    [Tooltip("是否自动播放")]
    public bool autoPlay = true;
    
    [Tooltip("延迟播放时间")]
    public float delay = 0f;

    [Header("材质控制")]
    [Tooltip("目标材质（留空则使用Renderer的材质）")]
    public Material targetMaterial;
    
    [Tooltip("Lerp参数名称")]
    public string lerpPropertyName = "_Lerp";
    

    private Renderer _renderer;
    private float _currentLerpValue;
    private float _animationDirection = 1f;
    private float _delayTimer;
    private bool _isPlaying;

    private void Awake()
    {
        // 获取Renderer组件
        _renderer = GetComponent<Renderer>();
        
        // 如果没有指定材质，则使用Renderer的材质
        if (targetMaterial == null && _renderer != null)
        {
            targetMaterial = _renderer.material;
        }
    }

    private void Start()
    {

        // 设置初始Lerp值
        _currentLerpValue = reverse ? 1f : 0f;
        UpdateMaterialLerp();

        // 如果设置了自动播放
        if (autoPlay)
        {
            _delayTimer = delay;
        }
    }

    private void Update()
    {
        // 处理延迟
        if (_delayTimer > 0)
        {
            _delayTimer -= Time.deltaTime;
            if (_delayTimer <= 0)
            {
                _isPlaying = true;
            }
            return;
        }

        // 如果不在播放状态，直接返回
        if (!_isPlaying) return;

        // 更新Lerp值
        _currentLerpValue += _animationDirection * animationSpeed * Time.deltaTime;

        // 检查边界并处理循环
        if (_currentLerpValue >= 1f)
        {
            _currentLerpValue = 1f;
            if (loopAnimation)
            {
                _animationDirection *= -1f;
            }
            else
            {
                _isPlaying = false;
            }
        }
        else if (_currentLerpValue <= 0f)
        {
            _currentLerpValue = 0f;
            if (loopAnimation)
            {
                _animationDirection *= -1f;
            }
            else
            {
                _isPlaying = false;
            }
        }

        // 更新材质的Lerp参数
        UpdateMaterialLerp();
    }

    private void UpdateMaterialLerp()
    {
        if (targetMaterial != null && !string.IsNullOrEmpty(lerpPropertyName))
        {
            targetMaterial.SetFloat(lerpPropertyName, _currentLerpValue);
        }
    }

    [ContextMenu("Play Animation")]
    public void Play()
    {
        _isPlaying = true;
        _delayTimer = 0;
    }

    [ContextMenu("Pause Animation")]
    public void Pause()
    {
        _isPlaying = false;
    }

    [ContextMenu("Stop Animation")]
    public void Stop()
    {
        _isPlaying = false;
        _currentLerpValue = reverse ? 1f : 0f;
        UpdateMaterialLerp();
    }

    [ContextMenu("Restart Animation")]
    public void Restart()
    {
        _currentLerpValue = reverse ? 1f : 0f;
        _animationDirection = reverse ? -1f : 1f;
        _isPlaying = true;
        _delayTimer = 0;
        UpdateMaterialLerp();
    }

    [ContextMenu("Toggle Play/Pause")]
    public void TogglePlayPause()
    {
        _isPlaying = !_isPlaying;
    }
}