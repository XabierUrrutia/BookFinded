using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LibraryARManager : MonoBehaviour
{

    [Header("UI 3D Flotante")]
    public GameObject infoCard3DPrefab; // Arrastra aquí el prefab que acabas de crear
    private GameObject currentInfoCardInstance; // La tarjeta que está activa en ese momento

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

    void Update()
    {
        // Detectar toque en pantalla (Móvil o Ratón)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Lanzamos el rayo
            if (Physics.Raycast(ray, out hit))
            {
                // ¿Hemos tocado un libro interactivo?
                BookInteractive clickedBook = hit.transform.GetComponent<BookInteractive>();

                if (clickedBook != null)
                {
                    Debug.Log($"👆 Tocado libro en Fila {clickedBook.row}, Col {clickedBook.column}");
                    SelectBookFrom3D(clickedBook.row, clickedBook.column, clickedBook.transform);
                }
            }
        }
    }

    // --- MÉTODO ACTUALIZADO PARA UI 3D ---
    void OnBookSelected(BookData book, Transform bookTransform)
    {
        // 1. Guardamos el libro actual y llenamos la UI 2D por si acaso (pero no la mostramos)
        currentSelectedBook = book;
        if (detailTitleText != null) detailTitleText.text = book.title;
        if (detailAuthorText != null) detailAuthorText.text = $"Autor: {book.author}";
        if (detailDescriptionText != null) detailDescriptionText.text = book.description;

        // 2. Aseguramos que NO salga el canvas 2D grande
        if (bookDetailCanvas != null) bookDetailCanvas.SetActive(false);

        // --- NUEVA LÓGICA 3D ---

        // a) Si ya había una tarjeta flotando, la borramos
        if (currentInfoCardInstance != null) Destroy(currentInfoCardInstance);

        // b) Si el libro NO está reservado, creamos la tarjeta 3D
        if (!book.isReserved)
        {
            currentInfoCardInstance = Instantiate(infoCard3DPrefab, bookTransform);

            currentInfoCardInstance.transform.localPosition = new Vector3(-1f, 0, 0);
            currentInfoCardInstance.transform.localScale = new Vector3(0.01f, 0.009f, 0.01f);

            var allTexts = currentInfoCardInstance.GetComponentsInChildren<TextMeshProUGUI>();

            foreach (var txt in allTexts)
            {
                // Opción A: Si pusiste los nombres correctos en el Prefab
                if (txt.name == "TitleText3D") txt.text = book.title;
                else if (txt.name == "AuthorText3D") txt.text = book.author;

                // Opción B (Salvavidas): Si NO cambiaste los nombres y se llaman "Text (TMP)"
                // Asumimos que el texto con la letra más grande es el Título
                else if (txt.fontSize > 5) // Ajusta este número según tus tamaños
                {
                    // Si no hemos asignado título aún, suponemos que es este
                    if (txt.text == "Titulo" || txt.text.Contains("New Text")) txt.text = book.title;
                }
                else
                {
                    if (txt.text == "Autor" || txt.text.Contains("New Text")) txt.text = book.author;
                }
            }
        }

        Debug.Log($"Seleccionado en 3D: {book.title}. Tarjeta creada.");
    }


    // Cambia la definición de SelectBookFrom3D por esta:
    void SelectBookFrom3D(int row, int col, Transform bookTransform)
    {
        if (currentBookshelf == null) return;
        BookData foundBook = currentBookshelf.books.Find(b => b.row == row && b.column == col);

        if (foundBook != null)
        {
            // Pasamos el libro y SU TRANSFORM (su posición en el mundo)
            OnBookSelected(foundBook, bookTransform);

            HighlightBookIn3D();
        }
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
        // 1. Verificación de seguridad
        if (currentBookshelf == null || currentTargetTransform == null)
        {
            return;
        }

        // 2. Cerrar UI
        if (shelfCanvas != null) shelfCanvas.SetActive(false);
        if (bookDetailCanvas != null) bookDetailCanvas.SetActive(false);

        // 3. Sacar la estantería (Esto pintará los reservados en ROJO automáticamente)
        SpawnOrActivateBookshelf3D();

        // 4. Lógica de resaltado (Dorado/Verde)
        if (currentSelectedBook != null)
        {
            // --- EL CAMBIO ESTÁ AQUÍ ---

            // Solo lo iluminamos ("Aquí está") si NO está reservado
            if (currentSelectedBook.isReserved == false)
            {
                HighlightBookIn3D();
            }
            else
            {
                Debug.Log($"El libro '{currentSelectedBook.title}' está reservado. No se marca en dorado.");
                // Al no llamar a HighlightBookIn3D, se quedará con el color Rojo 
                // que le puso el método SpawnOrActivateBookshelf3D.
            }
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

            // --- CORRECCIÓN DE POSICIÓN Y ROTACIÓN (TU CÓDIGO) ---
            current3DModelInstance.transform.localPosition = Vector3.zero;

            // Mantenemos TU rotación exacta:
            current3DModelInstance.transform.localRotation = Quaternion.Euler(180, 0, 0);


            // --- NUEVO BLOQUE: PINTAR LOS RESERVADOS ---
            // Busamos el visualizador (usamos InChildren por si usaste el contenedor, es más seguro)
            var visualizer = current3DModelInstance.GetComponentInChildren<BookshelfVisualizer>();

            if (visualizer != null)
            {
                // Le pasamos la lista de libros para que sepa cuáles pintar de rojo
                visualizer.InitializeShelfStatus(currentBookshelf.books);
            }
            // ---------------------------------------------
        }
    }

    void HighlightBookIn3D()
    {
        if (current3DModelInstance == null || currentSelectedBook == null) return;

        var visualizer = current3DModelInstance.GetComponentInChildren<BookshelfVisualizer>();

        if (visualizer != null)
        {
            // AHORA LE PASAMOS TAMBIÉN SI ES RESERVADO O NO
            visualizer.HighlightBook(
                currentSelectedBook.row,
                currentSelectedBook.column,
                currentSelectedBook.isReserved
            );
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