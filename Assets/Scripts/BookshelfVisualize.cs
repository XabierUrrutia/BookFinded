using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BookshelfVisualizer : MonoBehaviour
{
    [System.Serializable]
    public struct BookLocation
    {
        public int row;
        public int column;
        public Renderer bookRenderer;
    }

    [Header("Colores")]
    public Color highlightColor = Color.green;
    public Color reservedColor = Color.red;

    [Header("Animación")]
    public float popDistance = 0.15f; // Cuánto sale el libro hacia afuera
    public float animSpeed = 5f;

    public List<BookLocation> bookMap = new List<BookLocation>();

    private Renderer currentHighlighted;
    private Transform currentAnimatedTransform; // El libro que se está moviendo
    private Vector3 originalPosition;         // Su posición original
    private Color savedColor;

    // --- INICIALIZACIÓN (Pinta rojos) ---
    public void InitializeShelfStatus(List<BookData> booksInShelf)
    {
        foreach (var book in booksInShelf)
        {
            if (book.isReserved)
            {
                var target = GetLocation(book.row, book.column);
                if (target.bookRenderer != null)
                {
                    target.bookRenderer.material.color = reservedColor;
                }
            }
        }
    }

    // --- MÉTODO PRINCIPAL DE RESALTADO ---
    public void HighlightBook(int row, int col, bool isReserved)
    {
        // 1. Si ya había uno sacado, lo devolvemos a su sitio
        ResetHighlight();

        var target = GetLocation(row, col);
        if (target.bookRenderer == null) return;

        // Guardamos referencias
        currentHighlighted = target.bookRenderer;
        currentAnimatedTransform = target.bookRenderer.transform;
        originalPosition = currentAnimatedTransform.localPosition;
        savedColor = currentHighlighted.material.color;

        if (isReserved)
        {
            // SI ESTÁ RESERVADO: No cambia de color, solo vibra (Shake)
            Debug.Log("⛔ Libro reservado. Iniciando vibración.");
            StartCoroutine(AnimateShake(currentAnimatedTransform));
        }
        else
        {
            // SI ESTÁ DISPONIBLE: Cambia a verde y SALE hacia afuera (Pop-Out)
            currentHighlighted.material.color = highlightColor;

            // Calculamos la posición hacia afuera (Forward local del objeto)
            // Si sale hacia atrás, cambia 'Vector3.back' por 'Vector3.forward'
            Vector3 targetPos = originalPosition + (Vector3.back * popDistance);

            StartCoroutine(AnimateMove(currentAnimatedTransform, targetPos));
        }
    }

    // --- RESET ---
    public void ResetHighlight()
    {
        if (currentAnimatedTransform != null)
        {
            StopAllCoroutines();

            // --- NUEVO: Si el libro tenía una tarjeta hija, la destruimos ---
            foreach (Transform child in currentAnimatedTransform)
            {
                if (child.name.Contains("InfoCard3D")) // Asegúrate que el prefab contiene este nombre
                {
                    Destroy(child.gameObject);
                }
            }
            // ---------------------------------------------------------------

            if (currentHighlighted != null)
                currentHighlighted.material.color = savedColor;

            currentAnimatedTransform.localPosition = originalPosition;

            currentHighlighted = null;
            currentAnimatedTransform = null;
        }
    }

    // --- AYUDANTES ---
    private BookLocation GetLocation(int r, int c)
    {
        return bookMap.Find(b => b.row == r && b.column == c);
    }

    // --- CORRUTINAS DE ANIMACIÓN ---
    IEnumerator AnimateMove(Transform obj, Vector3 targetPos)
    {
        while (Vector3.Distance(obj.localPosition, targetPos) > 0.001f)
        {
            obj.localPosition = Vector3.Lerp(obj.localPosition, targetPos, Time.deltaTime * animSpeed);
            yield return null;
        }
        obj.localPosition = targetPos;
    }

    IEnumerator AnimateShake(Transform obj)
    {
        Vector3 startPos = obj.localPosition;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            // Vibra aleatoriamente
            float x = Random.Range(-0.02f, 0.02f);
            obj.localPosition = startPos + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.localPosition = startPos; // Vuelve al sitio
    }
}