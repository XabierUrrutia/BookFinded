using UnityEngine;
using Vuforia;

public class QRConnector : MonoBehaviour
{
    [Header("Referencias")]
    public LibraryARManager libraryManager;

    [Header("Configuración QR")]
    public string qrImageTargetName; // Nombre del Image Target en Vuforia

    void Start()
    {
        // Si no se asignó nombre, usar el nombre del GameObject
        if (string.IsNullOrEmpty(qrImageTargetName))
        {
            qrImageTargetName = gameObject.name;
        }

        // Suscribirse a eventos de Vuforia
        ObserverBehaviour observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
            Debug.Log($"QRConector listo: {qrImageTargetName}");
        }
        else
        {
            Debug.LogError($"No se encontró ObserverBehaviour en {gameObject.name}");
        }
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus targetStatus)
    {
        if (libraryManager == null) return;

        if (targetStatus.Status == Status.TRACKED ||
            targetStatus.Status == Status.EXTENDED_TRACKED)
        {
            // CAMBIO: Pasamos el transform del objeto (el Image Target)
            libraryManager.OnQRCodeDetected(qrImageTargetName, this.transform);
        }
        else
        {
            libraryManager.OnQRCodeLost();
        }
    }

    void OnDestroy()
    {
        ObserverBehaviour observerBehaviour = GetComponent<ObserverBehaviour>();
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }
}