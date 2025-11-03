using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Component that applies actions from the save-load menu UI to the hex map.
/// </summary>
public class SaveLoadMenu : MonoBehaviour
{
    [SerializeField]
    HexGrid hexGrid;

    [SerializeField]
    UIDocument saveLoadPanel;

    private VisualElement root;
    private Label menuLabel;
    private Button actionButton;
    private TextField nameInput;
    private ScrollView listContent;
    private Button deleteButton;
    private Button cancelButton;

    private bool saveMode;

    const int mapFileVersion = 5;

    void OnEnable()
    {
        root = saveLoadPanel.rootVisualElement;

        menuLabel = root.Q<Label>("menu-label");
        actionButton = root.Q<Button>("action-button");
        nameInput = root.Q<TextField>("map-name-field");
        listContent = root.Q<ScrollView>("save-list");
        deleteButton = root.Q<Button>("delete-button");
        cancelButton = root.Q<Button>("cancel-button");

        actionButton.clicked += Action;
        deleteButton.clicked += Delete;
        cancelButton.clicked += Close;
    }

    private void OnDisable()
    {
        actionButton.clicked -= Action;
        deleteButton.clicked -= Delete;
        cancelButton.clicked -= Close;
    }

    public void Open(bool saveMode)
    {
        gameObject.SetActive(true);

        this.saveMode = saveMode;
        if (saveMode)
        {
            menuLabel.text = "Save Map";
            actionButton.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButton.text = "Load";
        }

        FillList();
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }

    public void SelectItem(string name) => nameInput.value = name;

    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        nameInput.value = "";
        FillList();
    }

    /// <summary>
    /// Заполняет ScrollView списком карт.
    /// </summary>
    void FillList()
    {
        // Вместо уничтожения GameObjects, просто очищаем контейнер VisualElement
        listContent.Clear();

        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);

        foreach (string path in paths)
        {
            Button itemButton = new()
            {
                text = Path.GetFileNameWithoutExtension(path)
            };

            itemButton.AddToClassList("save-list-item");

            itemButton.clicked += () => {
                SelectItem(itemButton.text);
            };

            listContent.Add(itemButton);
        }
    }

    string GetSelectedPath()
    {
        string mapName = nameInput.value;
        if (string.IsNullOrEmpty(mapName))
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    void Save(string path)
    {
        using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
        writer.Write(mapFileVersion);
        hexGrid.Save(writer);
    }

    void Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }
        using var reader = new BinaryReader(File.OpenRead(path));
        int header = reader.ReadInt32();
        if (header <= mapFileVersion)
        {
            hexGrid.Load(reader, header);
            HexMapCamera.ValidatePosition();
        }
        else
        {
            Debug.LogWarning("Unknown map format " + header);
        }
    }
}
