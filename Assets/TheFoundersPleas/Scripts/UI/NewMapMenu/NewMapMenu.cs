using TheFoundersPleas.World;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Component that applies actions from the new map menu UI to the hex map.
/// </summary>
public class NewMapMenu : MonoBehaviour
{
	[SerializeField]
    private HexGrid hexGrid;

	[SerializeField]
    private HexMapGenerator mapGenerator;

	[SerializeField]
    private UIDocument newMapPanel;
    private Toggle generateToggle;
    private Toggle wrappingToggle;
    private Button smallButton;
    private Button mediumButton;
    private Button largeButton;
    private Button cancelButton;

    private void OnEnable()
	{
		VisualElement root = newMapPanel.rootVisualElement;
        generateToggle = root.Q<Toggle>("generate-toggle");
        wrappingToggle = root.Q<Toggle>("wrapping-toggle");
        smallButton = root.Q<Button>("small-button");
        mediumButton = root.Q<Button>("medium-button");
        largeButton = root.Q<Button>("large-button");
        cancelButton = root.Q<Button>("cancel-button");

        smallButton.clicked += CreateSmallMap;
        mediumButton.clicked += CreateMediumMap;
        largeButton.clicked += CreateLargeMap;
        cancelButton.clicked += Close;
    }

    private void OnDisable()
    {
        smallButton.clicked -= CreateSmallMap;
        mediumButton.clicked -= CreateMediumMap;
        largeButton.clicked -= CreateLargeMap;
        cancelButton.clicked -= Close;
    }

    public void Open()
	{
		gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close()
	{
		gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

	public void CreateSmallMap() => CreateMap(20, 15);

	public void CreateMediumMap() => CreateMap(40, 30);

	public void CreateLargeMap() => CreateMap(80, 60);

    private void CreateMap(int x, int z)
	{
        bool generateMaps = generateToggle.value;
        bool wrapping = wrappingToggle.value;

        if (generateMaps)
		{
			mapGenerator.GenerateMap(x, z, wrapping);
		}
		else
		{
			hexGrid.CreateMap(x, z, wrapping);
		}
		HexMapCamera.ValidatePosition();
		Close();
	}
}
