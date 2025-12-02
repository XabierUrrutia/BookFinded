using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class BookInfoController : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject infoCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI authorText;
    public TextMeshProUGUI descriptionText;

    [Header("Mobile Settings")]
    public float touchRadius = 50f; // Radio para toques en pantalla
    public LayerMask touchLayer = -1;

    private bool targetVisible = false;
    private Camera arCamera;
    private RectTransform canvasRect;

    void Start()
    {
        Debug.Log("📱 MobileARBook iniciado - Optimizado para móvil");

        // Obtener la cámara AR (importante para móvil)
        arCamera = Camera.main;
        if (arCamera == null)
        {
            Debug.LogError("❌ No se encontró la cámara AR");
        }

        // Configurar Canvas para móvil
        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);

            // Guardar referencia al RectTransform para cálculos de UI
            canvasRect = infoCanvas.GetComponent<RectTransform>();

            // Ajustar escala para móvil
            infoCanvas.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            Debug.Log("✅ Canvas configurado para móvil");
        }
        else
        {
            Debug.LogError("❌ Asigna el Canvas en el inspector!");
        }

        // Asegurar collider MÁS GRANDE para toques en móvil
        if (!GetComponent<Collider>())
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 3f; // Más grande para móvil
            Debug.Log("📦 Collider ajustado para móvil (x3)");
        }

        // Hacer cubo más visible
        GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 1f); // Azul brillante
    }

    // Métodos llamados desde Image Target
    public void OnTargetDetected()
    {
        targetVisible = true;
        Debug.Log("🟢 Target detectado en móvil");

        // Feedback visual
        GetComponent<Renderer>().material.color = Color.green;

#if UNITY_ANDROID || UNITY_IOS
        Vibrar(100); // Pequeña vibración en móvil
#endif
    }

    public void OnTargetLost()
    {
        targetVisible = false;
        Debug.Log("🔴 Target perdido en móvil");
        GetComponent<Renderer>().material.color = Color.red;

        if (infoCanvas != null && infoCanvas.activeSelf)
        {
            infoCanvas.SetActive(false);
        }
    }

    void Update()
    {
        // Solo procesar toques si el target es visible
        if (!targetVisible) return;

        // DETECCIÓN DE TOQUES PARA MÓVIL
        bool touchDetected = false;
        Vector2 touchPosition = Vector2.zero;

        // Para Android/iOS: toques en pantalla
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    touchDetected = true;
                    touchPosition = touch.position.ReadValue();
                    Debug.Log($"📱 Toque detectado en: {touchPosition}");
                    break;
                }
            }
        }

        // Para Editor: ratón (solo pruebas)
#if UNITY_EDITOR
        if (!touchDetected && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            touchDetected = true;
            touchPosition = Mouse.current.position.ReadValue();
        }
#endif

        if (touchDetected)
        {
            ProcessTouch(touchPosition);
        }
    }

    void ProcessTouch(Vector2 screenPosition)
    {
        if (arCamera == null) arCamera = Camera.main;

        Debug.Log($"🎯 Procesando toque en móvil: {screenPosition}");

        // Método 1: Raycast tradicional
        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        // Debug visual
        Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, 20f, touchLayer))
        {
            Debug.Log($"🔍 Toque golpeó: {hit.collider.name}");

            if (hit.collider.gameObject == this.gameObject)
            {
                Debug.Log("✅ ¡TOQUE EN EL CUBO EN MÓVIL!");
                ToggleCanvas();
                return;
            }
        }

        // Método 2: Verificación por proximidad (para móvil)
        CheckProximityTouch(screenPosition);
    }

    void CheckProximityTouch(Vector2 screenPosition)
    {
        // Convertir posición de pantalla a posición en el mundo
        Vector3 worldTouchPos = arCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, 5f));

        // Calcular distancia entre el toque y el cubo
        float distance = Vector3.Distance(worldTouchPos, transform.position);

        Debug.Log($"📏 Distancia al cubo: {distance}");

        // Si está cerca, considerar como toque (más permisivo en móvil)
        if (distance < 2f)
        {
            Debug.Log("✅ Toque por proximidad en móvil");
            ToggleCanvas();
        }
    }

    void ToggleCanvas()
    {
        if (infoCanvas == null) return;

        bool newState = !infoCanvas.activeSelf;
        infoCanvas.SetActive(newState);

        Debug.Log($"📱 Canvas en móvil: {(newState ? "ACTIVADO" : "DESACTIVADO")}");

        if (newState)
        {
            UpdateBookInfo();

            // Feedback en móvil
#if UNITY_ANDROID || UNITY_IOS
            Vibrar(50);
#endif
        }
    }

    void UpdateBookInfo()
    {
        if (titleText != null)
        {
            titleText.text = "Ludwig van Beethoven";
            titleText.fontSize = 36; // Más grande para móvil
        }

        if (authorText != null)
        {
            authorText.text = "Massin, Jean";
            authorText.fontSize = 36;
        }

        if (descriptionText != null)
        {
            descriptionText.text = "Biografía del compositor...";
            descriptionText.fontSize = 36;
        }
    }

    // Vibrar en móvil (opcional)
    void Vibrar(long milliseconds = 100)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            
            if (vibrator != null)
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // Para iOS necesitarías un plugin nativo
#endif
    }

    // Método para cerrar desde botón
    public void CloseCanvas()
    {
        if (infoCanvas != null)
        {
            infoCanvas.SetActive(false);
            Debug.Log("📱 Canvas cerrado desde botón en móvil");
        }
    }
}