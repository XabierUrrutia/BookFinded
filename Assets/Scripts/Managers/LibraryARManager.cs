using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
    public UnityEngine.UI.Image themePanelImage;

    [Header("UI Detalle - Libro")]
    public GameObject bookDetailCanvas;
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailAuthorText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailMetadataText;
    public UnityEngine.UI.Image detailCoverImage;
    public UnityEngine.UI.Button closeDetailButton;

    [Header("UI Componentes")]
    public UnityEngine.UI.Button closeShelfButton;
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

    public void OnQRCodeDetected(string qrName)
    {
        Debug.Log($"QR detectado: {qrName}");

        if (shelfDictionary.TryGetValue(qrName, out BookshelfData shelf))
        {
            currentBookshelf = shelf;
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
        currentBookshelf = null;
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

    // ========== DETALLE DE LIBRO ==========

    public void ShowBookDetail(BookData book)
    {
        if (book == null) return;

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

        // Mostrar detalle
        shelfCanvas.SetActive(false);
        bookDetailCanvas.SetActive(true);

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