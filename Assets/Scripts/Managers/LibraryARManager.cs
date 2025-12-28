using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LibraryARManager : MonoBehaviour
{
    [Header("Configuración General")]
    public Camera arCamera;
    public float uiScaleMobile = 0.003f;
    public float uiScaleEditor = 0.001f;

    [Header("Datos")]
    public List<BookshelfData> allBookshelves = new List<BookshelfData>();
    private BookshelfData currentBookshelf;
    private Dictionary<string, BookshelfData> shelfDictionary = new Dictionary<string, BookshelfData>();

    [Header("UI Principal - Estantería")]
    public GameObject shelfCanvas;
    public TextMeshProUGUI shelfTitleText;
    public TextMeshProUGUI themeDescriptionText;
    public TextMeshProUGUI statsText;
    public Transform booksListContainer;
    public GameObject bookItemPrefab;
    public Image themePanelImage;

    [Header("UI Detalle - Libro")]
    public GameObject bookDetailCanvas;
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailAuthorText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailMetadataText;
    public Image detailCoverImage;
    public Button closeDetailButton;
    public Button showInARButton;

    private Transform currentTargetTransform; // La posición del QR actual
    private GameObject current3DModelInstance; // La estantería 3D instanciada
    private BookData currentSelectedBook; // El libro que estamos viendo

    [Header("UI Componentes")]
    public Button closeShelfButton;
    public TMP_InputField searchInputField;
    public TMP_Dropdown filterDropdown;

    private List<GameObject> currentBookItems = new List<GameObject>();

    void Start()
    {
        InitializeSystem();
        SetupEventListeners();
        HideAllUI();

        Debug.Log("Sistema de Biblioteca AR inicializado");
    }

    void InitializeSystem()
    {
        // Crear diccionario para búsqueda rápida por QR
        foreach (var shelf in allBookshelves)
        {
            if (!string.IsNullOrEmpty(shelf.qrImageName) && !shelfDictionary.ContainsKey(shelf.qrImageName))
            {
                shelfDictionary[shelf.qrImageName] = shelf;
            }
        }

        // Ajustar escala de UI según plataforma
        float scale = Application.isMobilePlatform ? uiScaleMobile : uiScaleEditor;
        if (shelfCanvas != null) shelfCanvas.transform.localScale = Vector3.one * scale;
        if (bookDetailCanvas != null) bookDetailCanvas.transform.localScale = Vector3.one * scale;

        // Posicionar UI frente a la cámara
        PositionUIForAR();
    }

    void SetupEventListeners()
    {
        if (closeShelfButton != null)
            closeShelfButton.onClick.AddListener(HideAllUI);

        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(ShowShelfUI);

        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchValueChanged);

        if (filterDropdown != null)
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);

        if (showInARButton != null)
            showInARButton.onClick.AddListener(OnShowInARClicked);
    }

    void PositionUIForAR()
    {
        // Posicionar los Canvas en un lugar visible
        Vector3 uiPosition = arCamera.transform.position + arCamera.transform.forward * 1.5f;
        uiPosition.y = arCamera.transform.position.y;

        if (shelfCanvas != null)
            shelfCanvas.transform.position = uiPosition;

        if (bookDetailCanvas != null)
            bookDetailCanvas.transform.position = uiPosition;
    }

    // ========== MÉTODOS PÚBLICOS PARA VUFORIA ==========

    public void OnQRCodeDetected(string qrName, Transform targetTransform)
    {
        // 1. Siempre actualizamos la posición (necesario para el botón 3D)
        currentTargetTransform = targetTransform;

        if (shelfDictionary.TryGetValue(qrName, out BookshelfData detectedShelf))
        {
            // --- AQUÍ ESTÁ EL ARREGLO ---

            // Si la estantería que acabamos de detectar es la MISMA que ya tenemos guardada...
            if (currentBookshelf == detectedShelf)
            {
                // ... ¡No hacemos nada! Dejamos la UI como esté (ya sea lista o detalle).
                return;
            }

            // -----------------------------

            // Solo si es una estantería DIFERENTE (o la primera vez), cargamos la UI
            Debug.Log($"Nueva estantería detectada: {qrName}");
            currentBookshelf = detectedShelf;

            // Limpieza del modelo 3D anterior si cambiamos de estantería
            if (current3DModelInstance != null)
            {
                Destroy(current3DModelInstance);
            }

            ShowShelfUI();
        }
        else
        {
            Debug.LogWarning($"No hay estantería configurada para el QR: {qrName}");
        }
    }

    public void OnQRCodeLost()
    {
        HideAllUI();

        // Ocultar modelo 3D si se pierde el target
        if (current3DModelInstance != null)
            current3DModelInstance.SetActive(false);

        currentBookshelf = null;
        currentTargetTransform = null;
    }

    // ========== GESTIÓN DE UI ==========

    void ShowShelfUI()
    {
        if (currentBookshelf == null) return;

        UpdateShelfUI();
        shelfCanvas.SetActive(true);
        bookDetailCanvas.SetActive(false);

        Debug.Log($"Mostrando estantería: {currentBookshelf.displayName}");
    }

    void UpdateShelfUI()
    {
        // Información principal
        shelfTitleText.text = currentBookshelf.displayName;
        themeDescriptionText.text = currentBookshelf.themeDescription;

        // Estadísticas
        int totalBooks = currentBookshelf.books.Count;
        int totalGenres = currentBookshelf.availableGenres.Count;
        statsText.text = $"{totalBooks} libros · {totalGenres} géneros";

        // Color de tema
        if (themePanelImage != null)
            themePanelImage.color = currentBookshelf.themeColor;

        // Configurar filtros
        SetupFilterDropdown();

        // Crear lista de libros
        PopulateBooksList(currentBookshelf.books);
    }

    void SetupFilterDropdown()
    {
        if (filterDropdown == null) return;

        filterDropdown.ClearOptions();

        // Opción "Todos"
        filterDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("Todos los géneros"));

        // Añadir géneros únicos
        foreach (string genre in currentBookshelf.availableGenres)
        {
            filterDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData(genre));
        }

        filterDropdown.value = 0;
        filterDropdown.RefreshShownValue();
    }

    void PopulateBooksList(List<BookData> books)
    {
        // Limpiar lista anterior
        ClearBooksList();

        // Crear items para cada libro
        foreach (BookData book in books)
        {
            CreateBookListItem(book);
        }
    }

    void CreateBookListItem(BookData book)
    {
        if (bookItemPrefab == null || booksListContainer == null) return;

        GameObject item = Instantiate(bookItemPrefab, booksListContainer);
        currentBookItems.Add(item);

        // Configurar UI del item
        BookItemUI itemUI = item.GetComponent<BookItemUI>();
        if (itemUI != null)
        {
            itemUI.Initialize(book, this);
        }
        else
        {
            // Configuración manual si no hay componente BookItemUI
            TextMeshProUGUI titleText = item.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null) titleText.text = book.title;

            TextMeshProUGUI authorText = item.transform.Find("AuthorText")?.GetComponent<TextMeshProUGUI>();
            if (authorText != null) authorText.text = book.author;

            UnityEngine.UI.Button button = item.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => ShowBookDetail(book));
            }
        }
    }

    void ClearBooksList()
    {
        foreach (GameObject item in currentBookItems)
        {
            Destroy(item);
        }
        currentBookItems.Clear();
    }

    void OnShowInARClicked()
    {
        // 1. Verificación de seguridad básica
        if (currentBookshelf == null || currentTargetTransform == null)
        {
            Debug.LogError("No se puede mostrar AR: Falta la estantería o el QR Target");
            return;
        }

        Debug.Log("Ocultando UI y mostrando modelo 3D...");

        // 2. FORZAR CIERRE DE UI (Aquí es donde decimos adiós a los Canvas)
        if (shelfCanvas != null) shelfCanvas.SetActive(false);
        if (bookDetailCanvas != null) bookDetailCanvas.SetActive(false);

        // 3. Instanciar o activar la estantería 3D
        SpawnOrActivateBookshelf3D();

        // 4. Resaltar el libro (si hay uno seleccionado)
        if (currentSelectedBook != null)
        {
            HighlightBookIn3D();
        }
    }

    void SpawnOrActivateBookshelf3D()
    {
        // Destruir el anterior si existe para evitar duplicados o errores
        if (current3DModelInstance != null)
        {
            Destroy(current3DModelInstance);
        }

        if (currentBookshelf.shelfPrefab3D != null)
        {
            // Instanciar como hijo del ImageTarget
            current3DModelInstance = Instantiate(currentBookshelf.shelfPrefab3D, currentTargetTransform);

            // --- CORRECCIÓN DE POSICIÓN Y ROTACIÓN ---
            current3DModelInstance.transform.localPosition = Vector3.zero;

            // AQUÍ CORREGIMOS QUE SALGA AL REVÉS
            // Prueba con (0, 180, 0) si está mirando hacia atrás.
            // Prueba con (180, 0, 0) si está literalmente cabeza abajo.
            // Prueba con (-90, 0, 0) si está tumbada en el suelo.

            // Opción A: Girar 180 grados en vertical (Lo más habitual)
            current3DModelInstance.transform.localRotation = Quaternion.Euler(180, 0, 0);

            // Si la Opción A no funciona, cambia los números de arriba.
        }
    }

    void HighlightBookIn3D()
    {
        if (current3DModelInstance == null) return;

        // CAMBIO: Usamos GetComponentInChildren para encontrar el script
        // aunque esté en el modelo hijo (dentro del contenedor)
        var visualizer = current3DModelInstance.GetComponentInChildren<BookshelfVisualizer>();

        if (visualizer != null)
        {
            Debug.Log($"🎨 Intentando colorear libro en Fila: {currentSelectedBook.row}, Col: {currentSelectedBook.column}");
            visualizer.HighlightBook(currentSelectedBook.row, currentSelectedBook.column);
        }
        else
        {
            Debug.LogError("❌ NO se encuentra el componente BookshelfVisualizer en el modelo 3D instanciado.");
        }
    }

    // ========== DETALLE DE LIBRO ==========

    public void ShowBookDetail(BookData book)
    {
        if (book == null) return;
        currentSelectedBook = book;
        detailTitleText.text = book.title;
        detailAuthorText.text = $"Autor: {book.author}";
        detailDescriptionText.text = book.description;

        // Metadatos adicionales
        string metadata = $"ISBN: {book.isbn}\n";
        metadata += $"Año: {book.publicationYear}\n";
        metadata += $"Género: {book.genre}\n";
        metadata += $"Tema: {book.theme}";
        detailMetadataText.text = metadata;

        // Imagen de portada
        if (book.coverImage != null && detailCoverImage != null)
        {
            detailCoverImage.sprite = book.coverImage;
            detailCoverImage.color = Color.white;
        }
        else if (detailCoverImage != null)
        {
            detailCoverImage.color = book.bookColor;
        }

        if (currentBookshelf != null && currentBookshelf.shelfPrefab3D != null)
            showInARButton.gameObject.SetActive(true);
        else
            showInARButton.gameObject.SetActive(false);

        // Mostrar detalle
        shelfCanvas.SetActive(false);
        bookDetailCanvas.SetActive(true);

        if (current3DModelInstance != null)
            current3DModelInstance.SetActive(false);    

        Debug.Log($"Mostrando detalle: {book.title}");
    }

    // ========== FILTRADO Y BÚSQUEDA ==========

    void OnSearchValueChanged(string searchText)
    {
        FilterBooks();
    }

    void OnFilterChanged(int filterIndex)
    {
        FilterBooks();
    }

    void FilterBooks()
    {
        if (currentBookshelf == null) return;

        string searchTerm = searchInputField?.text?.ToLower() ?? "";
        string selectedGenre = filterDropdown?.value > 0 ?
            filterDropdown.options[filterDropdown.value].text : "";

        List<BookData> filteredBooks = currentBookshelf.books.Where(book =>
        {
            bool matches = true;

            // Filtrar por búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                matches &= book.title.ToLower().Contains(searchTerm) ||
                          book.author.ToLower().Contains(searchTerm) ||
                          book.genre.ToLower().Contains(searchTerm);
            }

            // Filtrar por género
            if (!string.IsNullOrEmpty(selectedGenre))
            {
                matches &= book.genre == selectedGenre;
            }

            return matches;
        }).ToList();

        // Actualizar lista
        ClearBooksList();
        foreach (BookData book in filteredBooks)
        {
            CreateBookListItem(book);
        }

        Debug.Log($"Filtrado: {filteredBooks.Count} libros encontrados");
    }

    // ========== UTILIDADES ==========

    void HideAllUI()
    {
        if (shelfCanvas != null) shelfCanvas.SetActive(false);
        if (bookDetailCanvas != null) bookDetailCanvas.SetActive(false);

        // Limpiar búsqueda y filtros
        if (searchInputField != null) searchInputField.text = "";
        if (filterDropdown != null) filterDropdown.value = 0;

        Debug.Log("UI ocultada");
    }

    public Color GetGenreColor(string genre)
    {
        // Colores por género (puedes personalizarlos)
        return genre.ToLower() switch
        {
            "biografía" => new Color(0.95f, 0.6f, 0.2f),
            "ficción" => new Color(0.3f, 0.7f, 0.9f),
            "ciencia" => new Color(0.4f, 0.8f, 0.4f),
            "historia" => new Color(0.8f, 0.5f, 0.8f),
            "tecnología" => new Color(0.2f, 0.6f, 0.9f),
            "arte" => new Color(0.9f, 0.3f, 0.4f),
            "filosofía" => new Color(0.7f, 0.5f, 0.3f),
            _ => new Color(0.8f, 0.8f, 0.8f)
        };
    }
}