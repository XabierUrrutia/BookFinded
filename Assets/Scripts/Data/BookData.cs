using UnityEngine;

[CreateAssetMenu(fileName = "NewBook", menuName = "AR Library/Book Data")]
public class BookData : ScriptableObject
{
    [Header("Información Básica")]
    public string bookID;
    public string title;
    public string author;
    public string isbn;
    public int publicationYear;

    [Header("Categorización")]
    public string theme;          // Ej: "Biografías"
    public string genre;          // Ej: "Música"
    public string shelfID;        // A qué estantería pertenece

    [Header("Descripción")]
    [TextArea(3, 10)] public string description;
    [TextArea(2, 5)] public string shortDescription;

    [Header("Recursos Visuales")]
    public Sprite coverImage;
    public Color bookColor = Color.white;

    [Header("AR (Opcional)")]
    public GameObject arModel;      // Modelo 3D del libro
    public AudioClip audioDescription;

    [Header("Ubicación Física")]
    public int shelfNumber;        // Número de estantería real
    public int row;               // Fila en la estantería
    public int column;            // Columna en la estantería
}