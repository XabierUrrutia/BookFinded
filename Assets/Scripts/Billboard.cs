using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main; // Encuentra la cámara automáticamente
    }

    // LateUpdate se ejecuta después de que la cámara se haya movido
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Hacemos que el objeto mire a la cámara en cada frame
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}