using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBookshelf", menuName = "AR Library/Bookshelf Data")]
public class BookshelfData : ScriptableObject
{
    [Header("Recursos AR")]
    public GameObject shelfPrefab3D; // El prefab 3D de la estantería completa

    [Header("Identificación")]
    public string shelfID;
    public string displayName;
    public string qrImageName;     // Nombre EXACTO del Image Target en Vuforia

    [Header("Temática")]
    public string mainTheme;
    [TextArea(3, 8)] public string themeDescription;
    public Color themeColor = new Color(0.1f, 0.3f, 0.7f, 1f);
    public Sprite themeIcon;

    [Header("Contenido")]
    public List<BookData> books = new List<BookData>();

    [Header("Estadísticas")]
    public int totalBooks;
    public List<string> availableGenres = new List<string>();

    // Esto se ejecuta automáticamente en el editor
    private void OnValidate()
    {
        totalBooks = books.Count;

        // Obtener géneros únicos
        availableGenres.Clear();
        foreach (var book in books)
        {
            if (!string.IsNullOrEmpty(book.genre) && !availableGenres.Contains(book.genre))
            {
                availableGenres.Add(book.genre);
            }
        }
    }
}