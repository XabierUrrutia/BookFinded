using UnityEngine;
using System.Collections.Generic;

public class BookshelfVisualizer : MonoBehaviour
{
    [System.Serializable]
    public struct BookLocation
    {
        public int row;
        public int column;
        public Renderer bookRenderer; // Arrastra aquí el objeto 3D del libro
    }

    [Header("Configuración")]
    public Color highlightColor = Color.green; // Color para resaltar
    public List<BookLocation> bookMap = new List<BookLocation>(); // Mapeo manual

    private Renderer currentHighlighted;
    private Color originalColor;

    // Método para resaltar un libro específico
    public void HighlightBook(int row, int col)
    {
        // 1. Restaurar el anterior si existe
        ResetHighlight();

        // 2. Buscar el libro en el mapa
        BookLocation target = bookMap.Find(b => b.row == row && b.column == col);

        if (target.bookRenderer != null)
        {
            currentHighlighted = target.bookRenderer;
            originalColor = currentHighlighted.material.color; // Guardar color original
            currentHighlighted.material.color = highlightColor; // Aplicar nuevo color
        }
        else
        {
            Debug.LogWarning($"No se encontró modelo 3D para Fila {row} - Columna {col}");
        }
    }

    public void ResetHighlight()
    {
        if (currentHighlighted != null)
        {
            currentHighlighted.material.color = originalColor;
            currentHighlighted = null;
        }
    }
}