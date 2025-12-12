using UnityEngine;
using UnityEngine.UI;

public class ContentChecker : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔍 VERIFICANDO CONTENT...");

        // Buscar LibraryManager
        LibraryARManager manager = FindObjectOfType<LibraryARManager>();
        if (manager == null)
        {
            Debug.LogError("❌ NO hay LibraryManager en la escena");
            return;
        }

        Debug.Log($"✅ LibraryManager encontrado: {manager.name}");

        // Verificar booksListContainer
        if (manager.booksListContainer == null)
        {
            Debug.LogError("❌ booksListContainer está VACÍO en el Inspector");
            Debug.Log("💡 Solución: Arrastra el Content al campo Books List Container");
        }
        else
        {
            Debug.Log($"✅ booksListContainer asignado: {manager.booksListContainer.name}");
            Debug.Log($"   Hijos: {manager.booksListContainer.childCount}");

            // Verificar componentes
            CheckContentComponents(manager.booksListContainer);
        }

        // Verificar bookItemPrefab
        if (manager.bookItemPrefab == null)
        {
            Debug.LogError("❌ bookItemPrefab está VACÍO");
            Debug.Log("💡 Solución: Arrastra BookItemPrefab.prefab desde carpeta Prefabs");
        }
        else
        {
            Debug.Log($"✅ bookItemPrefab asignado: {manager.bookItemPrefab.name}");
        }
    }

    void CheckContentComponents(Transform content)
    {
        // Verificar Vertical Layout Group
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            Debug.LogError("❌ Content NO tiene Vertical Layout Group");
            Debug.Log("💡 Solución: Añade componente Vertical Layout Group");
        }
        else
        {
            Debug.Log($"✅ Vertical Layout Group: Spacing={vlg.spacing}");
        }

        // Verificar Content Size Fitter
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            Debug.LogError("❌ Content NO tiene Content Size Fitter");
            Debug.Log("💡 Solución: Añade componente Content Size Fitter");
        }
        else
        {
            Debug.Log($"✅ Content Size Fitter: VerticalFit={csf.verticalFit}");
        }

        // Verificar RectTransform
        RectTransform rt = content.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"📐 Tamaño Content: {rt.rect.width}x{rt.rect.height}");
        }
    }

    [ContextMenu("Crear Content Automáticamente")]
    void CreateContentAuto()
    {
        Debug.Log("🏗️ Creando Content automáticamente...");

        // Buscar BooksScrollView
        GameObject scrollView = GameObject.Find("BooksScrollView");
        if (scrollView == null)
        {
            Debug.LogError("❌ No se encontró BooksScrollView");
            return;
        }

        // Buscar o crear Viewport
        Transform viewport = scrollView.transform.Find("Viewport");
        if (viewport == null)
        {
            Debug.Log("⚠️ No hay Viewport, creando uno...");
            GameObject vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollView.transform);
            vp.AddComponent<RectTransform>();
            vp.AddComponent<Mask>();
            viewport = vp.transform;

            // Configurar Viewport
            RectTransform vpRT = vp.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
        }

        // Crear Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport);

        // Añadir RectTransform
        RectTransform rt = content.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(10, -10);
        rt.offsetMax = new Vector2(-10, 10);

        // Añadir componentes necesarios
        content.AddComponent<VerticalLayoutGroup>();
        content.AddComponent<ContentSizeFitter>();

        Debug.Log($"✅ Content creado: {content.name}");
        Debug.Log("💡 Ahora arrástralo al campo Books List Container del LibraryManager");
    }
}