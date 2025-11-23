using System;
using System.IO;
using TheFoundersPleas.World;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Component that applies actions from the save-load menu UI to the hex map.
/// </summary>
public class SaveLoadMenu : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private UIDocument _saveLoadPanel;

    private HexGrid _hexGrid;
    private HexMapCamera _camera;

    private VisualElement _root;
    private Label _menuLabel;
    private Button _actionButton;
    private TextField _nameInput;
    private ScrollView _listContent;
    private Button _deleteButton;
    private Button _cancelButton;

    private bool _saveMode;
    private const int _mapFileVersion = 5;

    public void Initialize(HexGrid hexGrid, HexMapCamera mapCamera)
    {
        _hexGrid = hexGrid;
        _camera = mapCamera;
        _root = _saveLoadPanel.rootVisualElement;
    }

    private void OnEnable()
    {
        _menuLabel = _root.Q<Label>("menu-label");
        _actionButton = _root.Q<Button>("action-button");
        _nameInput = _root.Q<TextField>("map-name-field");
        _listContent = _root.Q<ScrollView>("save-list");
        _deleteButton = _root.Q<Button>("delete-button");
        _cancelButton = _root.Q<Button>("cancel-button");

        _actionButton.clicked += Action;
        _deleteButton.clicked += Delete;
        _cancelButton.clicked += Close;
    }

    private void OnDisable()
    {
        _actionButton.clicked -= Action;
        _deleteButton.clicked -= Delete;
        _cancelButton.clicked -= Close;
    }

    public void Open(bool saveMode)
    {
        gameObject.SetActive(true);

        this._saveMode = saveMode;
        if (saveMode)
        {
            _menuLabel.text = "Save Map";
            _actionButton.text = "Save";
        }
        else
        {
            _menuLabel.text = "Load Map";
            _actionButton.text = "Load";
        }

        FillList();
        _camera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        _camera.Locked = false;
    }

    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (_saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }

    public void SelectItem(string name) => _nameInput.value = name;

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
        _nameInput.value = "";
        FillList();
    }

    /// <summary>
    /// Заполняет ScrollView списком карт.
    /// </summary>
    private void FillList()
    {
        // Вместо уничтожения GameObjects, просто очищаем контейнер VisualElement
        _listContent.Clear();

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

            _listContent.Add(itemButton);
        }
    }

    private string GetSelectedPath()
    {
        string mapName = _nameInput.value;
        if (string.IsNullOrEmpty(mapName))
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    private void Save(string path)
    {
        using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
        writer.Write(_mapFileVersion);
        _hexGrid.Save(writer);
    }

    private void Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }
        using var reader = new BinaryReader(File.OpenRead(path));
        int header = reader.ReadInt32();
        if (header <= _mapFileVersion)
        {
            _hexGrid.Load(reader, header);
            _camera.ValidatePosition();
        }
        else
        {
            Debug.LogWarning("Unknown map format " + header);
        }
    }
}
