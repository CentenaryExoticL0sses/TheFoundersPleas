using TheFoundersPleas.World;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Component that applies actions from the new map menu UI to the hex map.
/// </summary>
public class NewMapMenu : MonoBehaviour
{
    [Header("Components")]
	[SerializeField] private UIDocument _newMapPanel;
    [SerializeField] private MapGeneratorConfig _config;

    private HexMapCreator _mapCreator;
    private HexMapCamera _mapCamera;

    private Toggle _generateToggle;
    private Toggle _wrappingToggle;
    private Button _smallButton;
    private Button _mediumButton;
    private Button _largeButton;
    private Button _cancelButton;

    public void Initialize(HexMapCreator mapCreator, HexMapCamera mapCamera)
    {
        _mapCreator = mapCreator;
        _mapCamera = mapCamera;
    }

    private void OnEnable()
    {
        VisualElement root = _newMapPanel.rootVisualElement;
        _generateToggle = root.Q<Toggle>("generate-toggle");
        _wrappingToggle = root.Q<Toggle>("wrapping-toggle");
        _smallButton = root.Q<Button>("small-button");
        _mediumButton = root.Q<Button>("medium-button");
        _largeButton = root.Q<Button>("large-button");
        _cancelButton = root.Q<Button>("cancel-button");

        _smallButton.clicked += CreateSmallMap;
        _mediumButton.clicked += CreateMediumMap;
        _largeButton.clicked += CreateLargeMap;
        _cancelButton.clicked += Close;
    }

    private void OnDisable()
    {
        _smallButton.clicked -= CreateSmallMap;
        _mediumButton.clicked -= CreateMediumMap;
        _largeButton.clicked -= CreateLargeMap;
        _cancelButton.clicked -= Close;
    }

    public void Open()
	{
		gameObject.SetActive(true);
        _mapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
        _mapCamera.Locked = false;
	}

	public void CreateSmallMap() => CreateMap(20, 15);

	public void CreateMediumMap() => CreateMap(40, 30);

	public void CreateLargeMap() => CreateMap(80, 60);

    private void CreateMap(int width, int height)
	{
        _config.Width = width;
        _config.Height = height;
        _config.GenerateMaps = _generateToggle.value;
        _config.Wrapping = _wrappingToggle.value;
        _mapCreator.CreateMap();
        _mapCamera.ValidatePosition();
		Close();
	}
}
