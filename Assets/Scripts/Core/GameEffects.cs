using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public sealed class GameEffects : MonoBehaviour
{
    [Header("Screen Shake")]
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float borderThickness = 30f;

    private Canvas overlayCanvas;
    private Image[] borderImages = new Image[4];
    private Coroutine flashRoutine;
    private Coroutine shakeRoutine;

    private static GameEffects instance;
    public static GameEffects Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new("GameEffects");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<GameEffects>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        CreateOverlayCanvas();
    }

    private void CreateOverlayCanvas()
    {
        GameObject canvasObj = new("VFX Overlay");
        DontDestroyOnLoad(canvasObj);
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRt = overlayCanvas.GetComponent<RectTransform>();

        string[] names = { "BorderTop", "BorderBottom", "BorderLeft", "BorderRight" };
        for (int i = 0; i < 4; i++)
        {
            GameObject bar = new(names[i]);
            bar.transform.SetParent(canvasObj.transform, false);
            Image img = bar.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;
            borderImages[i] = img;
        }

        LayoutBorder(borderThickness);
    }

    private void LayoutBorder(float thickness)
    {
        RectTransform canvasRt = overlayCanvas.GetComponent<RectTransform>();
        float w = canvasRt.rect.width;
        float h = canvasRt.rect.height;

        if (w <= 0 || h <= 0)
        {
            w = Screen.width;
            h = Screen.height;
        }

        SetupBorderRect(borderImages[0], 0, h - thickness, w, thickness);
        SetupBorderRect(borderImages[1], 0, 0, w, thickness);
        SetupBorderRect(borderImages[2], 0, 0, thickness, h);
        SetupBorderRect(borderImages[3], w - thickness, 0, thickness, h);
    }

    private static void SetupBorderRect(Image img, float x, float y, float w, float h)
    {
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    public void Warmup()
    {
    }

    public void DamageFlash()
    {
        FlashBorder(new Color(1f, 0.15f, 0.1f, 0.18f));
        ShakeScreen();
    }

    public void HealFlash()
    {
        FlashBorder(new Color(0.2f, 1f, 0.3f, 0.12f));
    }

    private void FlashBorder(Color color)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        LayoutBorder(borderThickness);
        for (int i = 0; i < 4; i++)
            borderImages[i].color = color;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            Color c = Color.Lerp(color, Color.clear, t * t);
            for (int i = 0; i < 4; i++)
                borderImages[i].color = c;
            yield return null;
        }
        for (int i = 0; i < 4; i++)
            borderImages[i].color = Color.clear;
    }

    public void ShakeScreen()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        TopDownCameraFollow camFollow = Camera.main != null ? Camera.main.GetComponent<TopDownCameraFollow>() : null;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;
            float magnitude = Mathf.Lerp(shakeMagnitude, 0f, t);
            Vector2 offset = Random.insideUnitCircle * magnitude;
            if (camFollow != null)
            {
                camFollow.ShakeOffset = new Vector3(offset.x, offset.y, 0f);
            }
            yield return null;
        }
        if (camFollow != null)
        {
            camFollow.ShakeOffset = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);
        if (instance == this)
            instance = null;
    }
}
