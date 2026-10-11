using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Экранное управление для телефонов: джойстик движения слева и кнопка взаимодействия справа.
/// Пишет в MobileInput, остальной код его не знает.
///
/// Создаётся само (RuntimeInitializeOnLoadMethod) в каждой сцене, где есть игрок (PlayerMovement),
/// поэтому в сцену его класть не нужно. Видно только на телефоне (MobileInput.IsMobile), и прячется,
/// когда смена закончилась (GameOverManager.IsGameOver) или игра на паузе (Time.timeScale == 0).
///
/// Касания читаются напрямую (Input.touches), без EventSystem: джойстик плавающий — появляется там,
/// где палец коснулся левой половины экрана; кнопка — круг в правом нижнем углу. Свой Canvas
/// с raycastTarget = false не перехватывает касания у интерфейса игры.
/// </summary>
public class TouchControls : MonoBehaviour
{
    // Размеры в единицах эталонного разрешения 1920×1080 (CanvasScaler ниже).
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    [SerializeField] private float joystickRadius = 150f;
    [SerializeField] private float knobRadius = 65f;
    [SerializeField] private Vector2 joystickRestPosition = new Vector2(300f, 270f);
    [Tooltip("Доля радиуса джойстика, внутри которой ввода нет.")]
    [SerializeField] private float deadZone = 0.2f;
    [SerializeField] private float buttonRadius = 120f;
    [SerializeField] private Vector2 buttonPosition = new Vector2(-260f, 250f); // от правого нижнего угла
    [Tooltip("Касание левее этой доли ширины экрана хватает джойстик.")]
    [SerializeField] private float joystickZoneWidth = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color baseColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color knobColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color buttonIdleColor = new Color(0.55f, 0.05f, 0.05f, 0.45f);
    [SerializeField] private Color buttonReadyColor = new Color(0.85f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private Color buttonPressedColor = new Color(1f, 0.25f, 0.25f, 1f);

    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _joystickBase;
    private RectTransform _knob;
    private Image _buttonImage;
    private RectTransform _button;
    private PlayerInteractor _player;

    private int _joystickFinger = -1;
    private int _buttonFinger = -1;
    private Vector2 _joystickOrigin; // в экранных пикселях

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, mode) => CreateIfPlayerScene();
        CreateIfPlayerScene();
    }

    private static void CreateIfPlayerScene()
    {
        MobileInput.Clear();
        if (FindAnyObjectByType<PlayerMovement>() == null || FindAnyObjectByType<TouchControls>() != null) return;
        new GameObject("TouchControls").AddComponent<TouchControls>();
    }

    private void Awake()
    {
        _player = FindAnyObjectByType<PlayerInteractor>();
        BuildUI();
        ResetJoystick();
    }

    private void OnEnable() => MobileInput.MobileChanged += RefreshVisible;
    private void OnDisable()
    {
        MobileInput.MobileChanged -= RefreshVisible;
        MobileInput.Clear();
    }

    private bool Blocked =>
        Time.timeScale == 0f || (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver);

    private void Update()
    {
        if (Input.touchCount > 0) MobileInput.NotifyTouch();
        RefreshVisible();
        if (!MobileInput.IsMobile) return;

        if (Blocked)
        {
            _joystickFinger = -1;
            _buttonFinger = -1;
            ResetJoystick();
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId == _joystickFinger) DragJoystick(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId == _joystickFinger)
                    {
                        _joystickFinger = -1;
                        ResetJoystick();
                    }
                    if (touch.fingerId == _buttonFinger) _buttonFinger = -1;
                    break;
            }
        }

        // Палец, которого больше нет на экране (событие Ended могло потеряться при сворачивании вкладки).
        if (Input.touchCount == 0 && _joystickFinger != -1)
        {
            _joystickFinger = -1;
            ResetJoystick();
        }
        if (Input.touchCount == 0) _buttonFinger = -1;

        UpdateButtonColor();
    }

    private void OnTouchBegan(Touch touch)
    {
        if (_buttonFinger == -1 && IsOverButton(touch.position))
        {
            _buttonFinger = touch.fingerId;
            MobileInput.PressInteract();
            return;
        }

        if (_joystickFinger == -1 && touch.position.x < Screen.width * joystickZoneWidth)
        {
            _joystickFinger = touch.fingerId;
            _joystickOrigin = touch.position;
            _joystickBase.anchoredPosition = ScreenToCanvas(touch.position);
            DragJoystick(touch.position);
        }
    }

    private void DragJoystick(Vector2 screenPosition)
    {
        float radiusPx = joystickRadius * CanvasScale;
        Vector2 offset = Vector2.ClampMagnitude(screenPosition - _joystickOrigin, radiusPx);
        Vector2 value = offset / radiusPx;

        _knob.anchoredPosition = offset / CanvasScale;
        MobileInput.SetMove(value.magnitude < deadZone ? Vector2.zero : value);
    }

    private void ResetJoystick()
    {
        MobileInput.SetMove(Vector2.zero);
        if (_joystickBase != null) _joystickBase.anchoredPosition = joystickRestPosition;
        if (_knob != null) _knob.anchoredPosition = Vector2.zero;
    }

    private bool IsOverButton(Vector2 screenPosition)
    {
        Vector2 center = RectTransformUtility.WorldToScreenPoint(null, _button.position);
        // Чуть шире видимого круга — по маленькому экрану легко промахнуться.
        return Vector2.Distance(screenPosition, center) <= buttonRadius * 1.25f * CanvasScale;
    }

    private void UpdateButtonColor()
    {
        if (_buttonImage == null) return;

        if (_buttonFinger != -1) _buttonImage.color = buttonPressedColor;
        else if (_player != null && _player.HasTarget) _buttonImage.color = buttonReadyColor;
        else _buttonImage.color = buttonIdleColor;
    }

    private void RefreshVisible()
    {
        if (_root == null) return;

        bool visible = MobileInput.IsMobile && !Blocked;
        if (_root.gameObject.activeSelf != visible) _root.gameObject.SetActive(visible);
    }

    private float CanvasScale => _canvas != null ? _canvas.scaleFactor : 1f;

    private Vector2 ScreenToCanvas(Vector2 screenPosition) => screenPosition / CanvasScale;

    // ---- Построение интерфейса в коде: в сцену ничего класть не нужно ----

    private void BuildUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50; // над HUD; экраны конца смены прячут управление через Blocked

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        _root = NewRect("Root", transform);
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        var disk = CircleSprite(ring: false);
        var ring = CircleSprite(ring: true);

        _joystickBase = NewImage("JoystickBase", _root, ring, baseColor, joystickRadius * 2f, Vector2.zero);
        _knob = NewImage("Knob", _joystickBase, disk, knobColor, knobRadius * 2f, new Vector2(0.5f, 0.5f));

        _button = NewImage("InteractButton", _root, disk, buttonIdleColor, buttonRadius * 2f, new Vector2(1f, 0f));
        _button.anchoredPosition = buttonPosition;
        _buttonImage = _button.GetComponent<Image>();
        NewImage("Ring", _button, ring, new Color(1f, 1f, 1f, 0.8f), buttonRadius * 2f, new Vector2(0.5f, 0.5f));

        var label = NewRect("Label", _button);
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = Vector2.zero;
        label.offsetMax = Vector2.zero;
        var text = label.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = "USE";
        text.fontSize = 56;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Круглая картинка. anchor — угол/центр родителя, от которого считается anchoredPosition.</summary>
    private static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color, float size, Vector2 anchor)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    /// <summary>Круг или кольцо со сглаженным краем — чтобы не тащить в проект текстуры.</summary>
    private static Sprite CircleSprite(bool ring)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size * 0.5f - 1f;
        float thickness = size * 0.05f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                float a = Mathf.Clamp01(r - d + 0.5f);
                if (ring) a *= Mathf.Clamp01(d - (r - thickness) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
