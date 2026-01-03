using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LibraryARManager : MonoBehaviour
{

    [Header("Efectos de Sonido")]
    public AudioSource audioSource; // El "altavoz"
    public AudioClip popSound;      // Arrastra aquí tu sonido de "Pop" o "Click"
    public AudioClip errorSound;    // Arrastra aquí tu sonido de "Error" o "Buzzer"

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
        // Detectar toque en pantalla
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // 1. ¿Hemos tocado un LIBRO?
                BookInteractive clickedBook = hit.transform.GetComponent<BookInteractive>();
                if (clickedBook != null)
                {
                    Debug.Log($"👆 Tocado libro en Fila {clickedBook.row}, Col {clickedBook.column}");
                    SelectBookFrom3D(clickedBook.row, clickedBook.column, clickedBook.transform);
                    return; // Importante: Salimos para no seguir comprobando
                }

                // 2. ¿Hemos tocado el BOTÓN CERRAR 3D? (NUEVO)
                CloseButtonInteractive closeBtn = hit.transform.GetComponent<CloseButtonInteractive>();
                if (closeBtn != null)
                {
                    // Llamamos a la función de cerrar que creamos antes
                    CloseEverything();
                }
            }
        }
    }

    // --- MÉTODO ACTUALIZADO PARA UI 3D ---
    void OnBookSelected(BookData book, Transform bookTransform)
    {
        // 1. Guardamos selección
        currentSelectedBook = book;
        if (detailTitleText != null) detailTitleText.text = book.title;
        if (bookDetailCanvas != null) bookDetailCanvas.SetActive(false);

        // --- LÓGICA DE SONIDO Y FICHA ---

        if (book.isReserved)
        {
            // CASO 1: RESERVADO (ERROR) ⛔
            Debug.Log("Libro reservado. Reproduciendo sonido de error.");

            if (audioSource != null && errorSound != null)
            {
                audioSource.PlayOneShot(errorSound);
            }

            // Opcional: Si está reservado, quizás NO queremos borrar la ficha anterior
            // o quizás sí. De momento, si está reservado, simplemente no hacemos nada más.
            return;
        }
        else
        {
            // CASO 2: DISPONIBLE (POP) ✅
            if (audioSource != null && popSound != null)
            {
                // PlayOneShot permite que se solapen sonidos si tocas muy rápido
                audioSource.PlayOneShot(popSound);
            }

            // --- LÓGICA DE CREAR LA TARJETA (Tu código anterior) ---

            // Si ya había una tarjeta, la borramos para poner la nueva
            if (currentInfoCardInstance != null) Destroy(currentInfoCardInstance);

            // Creamos la nueva tarjeta hija de la estantería
            currentInfoCardInstance = Instantiate(infoCard3DPrefab, current3DModelInstance.transform);

            // Posición fija abajo (la que ajustamos antes)
            currentInfoCardInstance.transform.localPosition = new Vector3(1.5f, 0, 1f);
            currentInfoCardInstance.transform.localScale = new Vector3(0.008f, 0.006f, 0.008f);

            // Rellenar textos
            var allTexts = currentInfoCardInstance.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var txt in allTexts)
            {
                if (txt.name == "TitleText3D") txt.text = book.title;
                else if (txt.name == "AuthorText3D") txt.text = book.author;
                else if (txt.fontSize > 5)
                {
                    if (txt.text == "Titulo" || txt.text.Contains("New Text")) txt.text = book.title;
                }
                else
                {
                    if (txt.text == "Autor" || txt.text.Contains("New Text")) txt.text = book.author;
                }
            }
        }
    }

    // Pequeña ayuda para saber si el texto es el por defecto
    bool IsGenericText(string t)
    {
        return t == "Titulo" || t == "Autor" || t.Contains("New Text");
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

        // 1. CONFIGURACIÓN VISUAL (Rellenamos datos)
        // Intentamos usar tu script auxiliar si existe para poner portada, autor, etc.
        BookItemUI itemUI = item.GetComponent<BookItemUI>();
        if (itemUI != null)
        {
            itemUI.Initialize(book, this);
        }
        else
        {
            // Configuración manual de respaldo
            TextMeshProUGUI authorText = item.transform.Find("AuthorText")?.GetComponent<TextMeshProUGUI>();
            if (authorText != null) authorText.text = book.author;
        }

        // 2. BUSCAMOS LOS COMPONENTES CLAVE
        UnityEngine.UI.Button itemButton = item.GetComponent<UnityEngine.UI.Button>();
        TextMeshProUGUI titleText = item.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();

        // 3. LÓGICA DE BLOQUEO Y COLOR (Aquí está la magia)
        if (book.isReserved)
        {
            // --- CASO: RESERVADO ⛔ ---

            // A) Ponemos el texto en ROJO
            if (titleText != null)
            {
                titleText.text = $"<color=red>{book.title} (Reservado)</color>";
            }

            // B) Controlamos el clic
            if (itemButton != null)
            {
                // Limpiamos cualquier listener que haya puesto el BookItemUI para que no abra nada
                itemButton.onClick.RemoveAllListeners();

                // Añadimos SOLO el sonido de error (o nada si prefieres silencio)
                itemButton.onClick.AddListener(() =>
                {
                    Debug.Log("⛔ Intento de acceso a libro reservado bloqueado.");
                    if (audioSource != null && errorSound != null)
                    {
                        audioSource.PlayOneShot(errorSound);
                    }
                });

                // OPCIONAL: Si prefieres que el botón se vea gris y no se pueda ni clicar:
                // itemButton.interactable = false; 
            }
        }
        else
        {
            // --- CASO: DISPONIBLE ✅ ---

            // A) Texto normal
            if (titleText != null) titleText.text = book.title;

            // B) Si no usas BookItemUI, necesitamos asignar el clic aquí.
            // Si usas BookItemUI, es probable que ya haya asignado el clic en el 'Initialize'.
            // Para asegurar que funciona siempre, forzamos el clic correcto aquí:
            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners(); // Limpieza por seguridad
                itemButton.onClick.AddListener(() => ShowBookDetail(book));
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
        if (currentBookshelf == null || currentTargetTransform == null) return;

        // 2. Cerrar UI 2D
        if (shelfCanvas != null) shelfCanvas.SetActive(false);
        if (bookDetailCanvas != null) bookDetailCanvas.SetActive(false);

        // 3. Sacar la estantería
        SpawnOrActivateBookshelf3D();

        // 4. Lógica de resaltado y TARJETA
        if (currentSelectedBook != null)
        {
            if (currentSelectedBook.isReserved == false)
            {
                // A) Iluminamos el libro (Visual)
                HighlightBookIn3D();

                // B) --- AQUÍ ESTABA EL FALLO ---
                // Forzamos que aparezca la tarjeta de información inmediatamente.
                // Pasamos 'null' como transform porque en tu nueva lógica la tarjeta
                // se pega a la estantería, no al libro, así que no necesita el transform del libro.
                OnBookSelected(currentSelectedBook, null);
            }
            else
            {
                Debug.Log("El libro está reservado. Solo se muestra la estantería.");
                // Opcional: Si quieres que suene el error también aquí:
                if (audioSource != null && errorSound != null) audioSource.PlayOneShot(errorSound);
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
            current3DModelInstance.transform.localPosition = new Vector3(-1f, 0, 0);

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

    public void CloseEverything()
    {
        // Borrar estantería
        if (current3DModelInstance != null)
        {
            Destroy(current3DModelInstance);
            current3DModelInstance = null;
        }

        // Borrar tarjeta flotante
        if (currentInfoCardInstance != null)
        {
            Destroy(currentInfoCardInstance);
            currentInfoCardInstance = null;
        }

        // Limpiar selección y UI
        currentSelectedBook = null;
        HideAllUI();

        Debug.Log("❌ Estantería cerrada desde el botón 3D.");
    }
}