using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 桌宠拖拽组件 (3D版本) - 简洁可靠
/// </summary>
public class DesktopPetDrag : MonoBehaviour
{
    [Header("拖拽设置")]
    [Tooltip("是否允许拖拽")]
    public bool canDrag = true;

    [Header("移动设置")]
    [Tooltip("是否平滑移动")]
    public bool smoothMove = true;

    [Tooltip("平滑移动速度")]
    public float smoothSpeed = 15f;

    [Header("缩放设置")]
    [Tooltip("最小缩放")]
    public float minScale = 0.5f;
    
    [Tooltip("最大缩放")]
    public float maxScale = 2f;

    [Tooltip("滚轮缩放速度")]
    public float zoomSpeed = 0.1f;

    // 内部变量
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 targetPosition;
    
    // 拖拽相关
    private Vector3 dragOffset;        // 鼠标点击点到物体的偏移
    private Vector3 startMousePos;     // 开始拖拽时的鼠标位置（世界坐标）
    private Vector3 startObjPos;      // 开始拖拽时的物体位置
    
    // Input System 相关
    private Vector2 currentMousePos;
    private Vector2 lastMousePos;
    private bool useInputSystem;

    // 选择相关
    private static DesktopPetDrag selectedObject;
    private static DesktopPetDrag hoveredObject;
    private bool isSelected = false;

    // 事件
    public System.Action OnDragStart;
    public System.Action OnDragEnd;
    public System.Action OnSelect;
    public System.Action OnDeselect;

    private void Awake()
    {
        mainCamera = Camera.main;
        targetPosition = transform.position;
        
        // 检测是否使用 Input System
        useInputSystem = false;
    }

    private void Start()
    {
        // 尝试检测 Input System
        try
        {
            var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                var mouse = mouseType.GetProperty("current")?.GetValue(null);
                if (mouse != null)
                {
                    useInputSystem = true;
                }
            }
        }
        catch
        {
            useInputSystem = false;
        }
    }

    private void Update()
    {
        // 获取鼠标位置
        UpdateMousePosition();

        // 处理鼠标按下
        if (canDrag && IsLeftButtonDown())
        {
            TryStartDrag();
            TrySelectObject();
        }

        // 处理鼠标释放
        if (IsLeftButtonUp() && isDragging)
        {
            EndDrag();
        }

        // 处理拖拽中
        if (isDragging)
        {
            UpdateDrag();
        }

        // 检测悬停
        UpdateHover();

        // 处理缩放（只缩放选中的物体）
        HandleZoom();

        // 平滑移动（非拖拽时）
        if (smoothMove && !isDragging)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
        else if (!smoothMove && !isDragging)
        {
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// 更新鼠标位置
    /// </summary>
    private void UpdateMousePosition()
    {
        lastMousePos = currentMousePos;
        
        if (useInputSystem)
        {
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    currentMousePos = mouse.position.ReadValue();
                }
            }
            catch
            {
                currentMousePos = UnityEngine.Input.mousePosition;
            }
        }
        else
        {
            currentMousePos = UnityEngine.Input.mousePosition;
        }
    }

    /// <summary>
    /// 检测鼠标左键是否按下
    /// </summary>
    private bool IsLeftButtonDown()
    {
        if (useInputSystem)
        {
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    return mouse.leftButton.wasPressedThisFrame;
                }
            }
            catch { }
        }
        return UnityEngine.Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 检测鼠标左键是否释放
    /// </summary>
    private bool IsLeftButtonUp()
    {
        if (useInputSystem)
        {
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    return mouse.leftButton.wasReleasedThisFrame;
                }
            }
            catch { }
        }
        return UnityEngine.Input.GetMouseButtonUp(0);
    }

    /// <summary>
    /// 检测鼠标左键是否按住
    /// </summary>
    private bool IsLeftButtonHeld()
    {
        if (useInputSystem)
        {
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    return mouse.leftButton.isPressed;
                }
            }
            catch { }
        }
        return UnityEngine.Input.GetMouseButton(0);
    }

    /// <summary>
    /// 尝试开始拖拽
    /// </summary>
    private void TryStartDrag()
    {
        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                StartDrag(hit.point);
                return;
            }
        }
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    private void StartDrag(Vector3 hitPoint)
    {
        isDragging = true;
        
        // 记录初始位置
        startObjPos = transform.position;
        startMousePos = transform.position; // 使用物体位置作为鼠标的世界坐标基准
        
        // 计算偏移（点击点相对于物体的位置）
        dragOffset = startObjPos - hitPoint;

        OnDragStart?.Invoke();
    }

    /// <summary>
    /// 更新拖拽位置
    /// </summary>
    private void UpdateDrag()
    {
        // 将当前鼠标位置转换为世界坐标
        // 使用固定的Z深度（物体自身的Z）
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(currentMousePos.x, currentMousePos.y, startObjPos.z - mainCamera.transform.position.z)
        );
        
        // 新位置 = 鼠标位置 + 初始偏移
        Vector3 newPosition = mouseWorldPos + dragOffset;
        
        // 保持原始Z深度
        newPosition.z = startObjPos.z;

        // 立即移动
        transform.position = newPosition;
        targetPosition = newPosition;
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    private void EndDrag()
    {
        isDragging = false;
        targetPosition = transform.position;
        OnDragEnd?.Invoke();
    }

    /// <summary>
    /// 处理缩放（只缩放选中的物体）
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y / 120f;
        if (Mathf.Abs(scroll) > 0.001f && selectedObject == this)
        {
            Zoom(scroll > 0);
        }
    }

    /// <summary>
    /// 检测悬停
    /// </summary>
    private void UpdateHover()
    {
        if (!IsMouseOverThis()) return;

        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            var drag = hit.transform.GetComponent<DesktopPetDrag>();
            if (drag != null)
            {
                if (hoveredObject != drag)
                {
                    hoveredObject = drag;
                }
                return;
            }
        }
        hoveredObject = null;
    }

    /// <summary>
    /// 尝试选中对象
    /// </summary>
    private void TrySelectObject()
    {
        if (!IsMouseOverThis()) return;

        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            var drag = hit.transform.GetComponent<DesktopPetDrag>();
            if (drag != null)
            {
                // 取消之前选中的
                if (selectedObject != null && selectedObject != drag)
                {
                    selectedObject.Deselect();
                }

                // 选中新的
                selectedObject = drag;
                drag.Select();
                return;
            }
        }

        // 点击空白处，取消选中
        if (selectedObject != null)
        {
            selectedObject.Deselect();
            selectedObject = null;
        }
    }

    /// <summary>
    /// 检测鼠标是否在这个物体上
    /// </summary>
    private bool IsMouseOverThis()
    {
        Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 选中这个物体
    /// </summary>
    public void Select()
    {
        if (isSelected) return;
        isSelected = true;
        OnSelect?.Invoke();
    }

    /// <summary>
    /// 取消选中这个物体
    /// </summary>
    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;
        OnDeselect?.Invoke();
    }

    /// <summary>
    /// 检测当前物体是否被选中
    /// </summary>
    public bool IsSelected => isSelected;

    /// <summary>
    /// 获取当前选中的物体
    /// </summary>
    public static DesktopPetDrag GetSelected() => selectedObject;

    /// <summary>
    /// 缩放
    /// </summary>
    public void Zoom(bool zoomIn)
    {
        float delta = zoomIn ? zoomSpeed : -zoomSpeed;
        Vector3 newScale = transform.localScale + Vector3.one * delta;
        newScale = Vector3.Max(newScale, Vector3.one * minScale);
        newScale = Vector3.Min(newScale, Vector3.one * maxScale);
        transform.localScale = newScale;
    }

    /// <summary>
    /// 设置是否允许拖拽
    /// </summary>
    public void SetDragEnabled(bool enabled)
    {
        canDrag = enabled;
        if (!enabled && isDragging)
        {
            EndDrag();
        }
    }

    /// <summary>
    /// 获取当前是否正在拖拽
    /// </summary>
    public bool IsDragging => isDragging;

    private void OnDisable()
    {
        if (isDragging)
        {
            EndDrag();
        }
    }

    private void OnDestroy()
    {
        OnDragStart = null;
        OnDragEnd = null;
    }
}
