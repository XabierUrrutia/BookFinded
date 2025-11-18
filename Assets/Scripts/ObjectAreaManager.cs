using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;


[System.Serializable]
public class ObjectArea
{
    public string name;
    public Vector3 initialLocalPos;
    public Vector3 finalLocalPos;
}

[System.Serializable]
public class ObjectAreaList
{
    public List<ObjectArea> areas = new();
}

public class DraftArea
{
    public Vector3? draftInitial;
    public Vector3? draftFinal;

    public GameObject initialMarker;
    public GameObject finalMarker;

    public bool IsComplete => draftInitial.HasValue && draftInitial.HasValue;
}

public class RuntimeArea
{
    public ObjectArea data;
    public GameObject box;
}


public class ObjectAreaManager : MonoBehaviour
{
    [Header("References")]
    public Transform imageTargetTransform;
    public TMPro.TMP_InputField nameInput;
    public Transform cursor;

    public GameObject pointMarkerPrefab;
    public GameObject boxAreaPrefab;

    public UnityEvent onConfirmArea;
    public UnityEvent<RuntimeArea> onSavedArea;

    private ObjectAreaList savedAreas = new();
    private readonly List<RuntimeArea> runtimeAreas = new();

    private DraftArea draft = null;
    private string savePath;


    // Initialization
    void Start()
    {
        savePath = Application.persistentDataPath + "/areas.json";

        LoadFromFile();
        RecreateSavedBoxes();
    }


    // Load and Save
    private void LoadFromFile()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        savedAreas = JsonUtility.FromJson<ObjectAreaList>(json);

        Debug.Log("Loaded " + savedAreas.areas.Count + " saved areas.");
    }

    private void SaveToFile()
    {
        string json = JsonUtility.ToJson(savedAreas);
        File.WriteAllText(savePath, json);

        Debug.Log("Saved " + savedAreas.areas.Count + " areas.");
    }


    // Loading Saved Areas
    private void RecreateSavedBoxes()
    {
        foreach (ObjectArea area in savedAreas.areas)
        {
            GameObject box = CreateBoxBetween(area.initialLocalPos, area.finalLocalPos);

            runtimeAreas.Add(new RuntimeArea()
            {
                data = area,
                box = box
            });
        }
    }


    // Creating New Areas
    public void StartNewArea()
    {
        ClearDraftMarkers();
        draft = new DraftArea();

        Debug.Log("Started new object area.");
    }

    public void SetInitialPosition()
    {
        if (draft == null)
        {
            Debug.LogWarning("Start a new area first.");
            return;
        }

        Vector3 worldPos = cursor ? cursor.position : Camera.main.transform.position;
        Vector3 localPos = imageTargetTransform.InverseTransformPoint(worldPos);

        draft.draftInitial = localPos;
        CreateOrMoveDraftMarker(ref draft.initialMarker, localPos);

        Debug.Log("Initial draft point set.");
    }

    public void SetFinalPosition()
    {
        if (draft == null)
        {
            Debug.LogWarning("Start a new area first.");
            return;
        }

        Vector3 worldPos = cursor ? cursor.position : Camera.main.transform.position;
        Vector3 localPos = imageTargetTransform.InverseTransformPoint(worldPos);

        draft.draftFinal = localPos;
        CreateOrMoveDraftMarker(ref draft.finalMarker, localPos);

        Debug.Log("Final draft point set.");
    }

    public void ConfirmArea()
    {
        if (draft == null)
        {
            Debug.LogWarning("No active draft to confirm.");
            return;
        }

        if (!draft.IsComplete)
        {
            Debug.LogWarning("Draft area incomplete. Set both initial and final.");
            return;
        }

        Debug.Log("Area confirmed—awaiting name input.");
        onConfirmArea.Invoke();
    }

    public void SaveAreaWithName()
    {
        if (draft == null)
        {
            Debug.LogWarning("No draft to save.");
            return;
        }

        if (!draft.IsComplete)
        {
            Debug.LogWarning("Incomplete draft cannot be saved.");
            return;
        }

        if (string.IsNullOrWhiteSpace(nameInput.text))
        {
            Debug.LogWarning("Name is required before saving.");
            return;
        }

        // Build permanent area
        ObjectArea area = new()
        {
            name = nameInput.text.Trim(),
            initialLocalPos = draft.draftInitial.Value,
            finalLocalPos = draft.draftFinal.Value
        };

        savedAreas.areas.Add(area);
        SaveToFile();

        GameObject box = CreateBoxBetween(area.initialLocalPos, area.finalLocalPos);
        RuntimeArea runtimeArea = new()
        {
            data = area,
            box = box
        };
        runtimeAreas.Add(runtimeArea);

        ClearDraftMarkers();
        onSavedArea.Invoke(runtimeArea);

        Debug.Log("Area saved successfully.");
    }


    // Utilities
    private void CreateOrMoveDraftMarker(ref GameObject marker, Vector3 localPos)
    {
        if (marker == null)
        {
            marker = Instantiate(pointMarkerPrefab);
            marker.transform.SetParent(imageTargetTransform, true);
        }

        marker.transform.localPosition = localPos;
    }

    private void ClearDraftMarkers()
    {
        if (draft == null) return;

        if (draft.initialMarker) Destroy(draft.initialMarker);
        if (draft.finalMarker) Destroy(draft.finalMarker);

        nameInput.text = "";
        draft = null;
    }

    private GameObject CreateBoxBetween(Vector3 localStart, Vector3 localEnd)
    {
        GameObject box = Instantiate(boxAreaPrefab);
        box.transform.SetParent(imageTargetTransform, true);

        Vector3 center = (localStart + localEnd) / 2f;
        Vector3 direction = localEnd - localStart;
        float length = direction.magnitude;

        box.transform.SetLocalPositionAndRotation(
            center,
            Quaternion.LookRotation(direction)
        );
        box.transform.localScale = new Vector3(0.02f, 0.02f, length);

        return box;
    }
}
