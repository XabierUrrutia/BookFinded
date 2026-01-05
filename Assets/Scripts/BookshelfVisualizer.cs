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
    public float popDistance = 0.15f;
    public float animSpeed = 5f;

    public List<BookLocation> bookMap = new List<BookLocation>();

    private Renderer currentHighlighted;
    private Transform currentAnimatedTransform;
    private Vector3 originalPosition;
    private Color savedColor;

    // -----------------------------------------------------------------------
    // ¡¡ESTE ES EL MÉTODO QUE FALTABA!! AÑÁDELO AQUÍ
    // -----------------------------------------------------------------------
    public Transform GetBookTransform(int row, int col)
    {
        // Reutilizamos tu función GetLocation para buscar el libro
        var target = GetLocation(row, col);

        // Si existe y tiene Renderer, devolvemos su Transform (su posición)
        if (target.bookRenderer != null)
        {
            return target.bookRenderer.transform;
        }

        // Si no lo encuentra, devolvemos null
        return null;
    }
    // -----------------------------------------------------------------------

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
        ResetHighlight();

        var target = GetLocation(row, col);
        if (target.bookRenderer == null) return;

        currentHighlighted = target.bookRenderer;
        currentAnimatedTransform = target.bookRenderer.transform;
        originalPosition = currentAnimatedTransform.localPosition;
        savedColor = currentHighlighted.material.color;

        if (isReserved)
        {
            Debug.Log("⛔ Libro reservado. Iniciando vibración.");
            StartCoroutine(AnimateShake(currentAnimatedTransform));
        }
        else
        {
            currentHighlighted.material.color = highlightColor;
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

            foreach (Transform child in currentAnimatedTransform)
            {
                if (child.name.Contains("InfoCard3D"))
                {
                    Destroy(child.gameObject);
                }
            }

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
            float x = Random.Range(-0.02f, 0.02f);
            obj.localPosition = startPos + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.localPosition = startPos;
    }
}