using UnityEngine;
using System.Collections;

public class VuforiaCubeClick : MonoBehaviour
{
    public GameObject canvasAR;
    public float canvasDistance = 0.8f; // Mayor distancia para móvil

    private bool imageDetected = false;
    private Camera arCamera;

    void Start()
    {
        arCamera = Camera.main;

        if (canvasAR != null)
        {
            canvasAR.SetActive(false);
            Debug.Log("Canvas desactivado al inicio");
        }

        // Verificar componentes
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError("NO HAY COLLIDER en: " + gameObject.name);
            gameObject.AddComponent<BoxCollider>();
        }
    }

    // Llamar este método cuando Vuforia detecte la imagen
    public void OnImageDetected()
    {
        imageDetected = true;
        Debug.Log("Imagen detectada - Listo para touch");
    }

    // Llamar este método cuando Vuforia pierda la imagen
    public void OnImageLost()
    {
        imageDetected = false;
        if (canvasAR != null)
            canvasAR.SetActive(false);
    }

    void Update()
    {
        if (!imageDetected) return;

        // Detección mejorada de touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log("Touch detectado en posición: " + touch.position);
                CheckTouch(touch.position);
            }
        }
    }

    void CheckTouch(Vector2 touchPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        // Debug visual (solo en Editor)
#if UNITY_EDITOR
        Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 2f);
#endif

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Debug.Log("Raycast golpeó: " + hit.collider.gameObject.name);

            // Verificar si golpeamos ESTE cubo
            if (hit.collider.gameObject == this.gameObject)
            {
                Debug.Log("¡CUBO TOUCH DETECTADO!");
                ToggleCanvas();
            }
        }
        else
        {
            Debug.Log("Raycast no golpeó ningún objeto con collider");
        }
    }

    void ToggleCanvas()
    {
        if (canvasAR == null)
        {
            Debug.LogError("CanvasAR no asignado!");
            return;
        }

        bool wasActive = canvasAR.activeSelf;
        canvasAR.SetActive(!wasActive);

        Debug.Log("Canvas estado: " + canvasAR.activeSelf);

        if (!wasActive) // Si se acaba de activar
        {
            StartCoroutine(PositionCanvasWithDelay());
        }
    }

    IEnumerator PositionCanvasWithDelay()
    {
        // Pequeño delay para asegurar que el Canvas está activo
        yield return new WaitForEndOfFrame();

        PositionCanvas();
    }

    void PositionCanvas()
    {
        if (arCamera == null) return;

        // Posicionar canvas entre el cubo y la cámara
        Vector3 cameraPosition = arCamera.transform.position;
        Vector3 cubePosition = transform.position;

        Vector3 direction = (cameraPosition - cubePosition).normalized;
        Vector3 canvasPosition = cubePosition + (direction * canvasDistance);

        canvasAR.transform.position = canvasPosition;

        // Orientar hacia la cámara
        canvasAR.transform.LookAt(arCamera.transform);
        canvasAR.transform.Rotate(0, 180f, 0); // Corregir orientación

        Debug.Log("Canvas posicionado en: " + canvasPosition);
    }
}