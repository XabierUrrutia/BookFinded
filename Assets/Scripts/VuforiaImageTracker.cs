using UnityEngine;
using Vuforia;

public class VuforiaImageTracker : DefaultObserverEventHandler
{
    [Header("Referencias")]
    public VuforiaCubeClick cubeClickHandler;

    protected override void OnTrackingFound()
    {
        base.OnTrackingFound();
        Debug.Log("IMAGEN DETECTADA: " + gameObject.name);

        if (cubeClickHandler != null)
            cubeClickHandler.OnImageDetected();
        else
            Debug.LogWarning("CubeClickHandler no asignado en el inspector");
    }

    protected override void OnTrackingLost()
    {
        base.OnTrackingLost();
        Debug.Log("IMAGEN PERDIDA: " + gameObject.name);

        if (cubeClickHandler != null)
            cubeClickHandler.OnImageLost();
    }
}