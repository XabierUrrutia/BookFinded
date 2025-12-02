using UnityEngine;

public class ClickDebug : MonoBehaviour
{
    void Start()
    {
        // Hacer el cubo de un color llamativo
        GetComponent<Renderer>().material.color = Color.blue;
        Debug.Log("🎲 Cubo listo - Haz clic en mí!");
    }

    // Esto se ejecuta CADA VEZ que haces clic en el cubo
    void OnMouseDown()
    {
        Debug.Log("✅ ¡FUNCIONA! Has hecho clic en el cubo");

        // Cambiar de color para confirmar visualmente
        GetComponent<Renderer>().material.color =
            (GetComponent<Renderer>().material.color == Color.blue) ?
            Color.green : Color.blue;

        Debug.Log("🎨 Color cambiado");
    }
}