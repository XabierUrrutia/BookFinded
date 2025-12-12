using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BookItemUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI authorText;
    public TextMeshProUGUI genreText;
    public Image backgroundImage;
    public Image genreBadge;

    [Header("Configuración")]
    public Color defaultColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    private BookData bookData;
    private LibraryARManager libraryManager;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnBookItemClicked);
        }
    }

    public void Initialize(BookData data, LibraryARManager manager)
    {
        bookData = data;
        libraryManager = manager;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (bookData == null) return;

        // Textos
        if (titleText != null) titleText.text = bookData.title;
        if (authorText != null) authorText.text = bookData.author;
        if (genreText != null) genreText.text = bookData.genre;

        // Colores
        Color genreColor = libraryManager != null ?
            libraryManager.GetGenreColor(bookData.genre) : defaultColor;

        if (backgroundImage != null)
            backgroundImage.color = new Color(genreColor.r, genreColor.g, genreColor.b, 0.2f);

        if (genreBadge != null)
            genreBadge.color = genreColor;
    }

    void OnBookItemClicked()
    {
        if (bookData != null && libraryManager != null)
        {
            libraryManager.ShowBookDetail(bookData);
        }
    }
}