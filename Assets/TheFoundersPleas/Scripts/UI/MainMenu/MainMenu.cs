using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private UIDocument _menuPanel;

    private const string NewGameButton = "NewGameButton";
    private const string LoadButton = "LoadButton";
    private const string SettingsButton = "SettingsButton";
    private const string ExitButton = "ExitButton";

    private void OnEnable()
    {
        VisualElement root = _menuPanel.rootVisualElement;

        root.Q<Button>(NewGameButton).clicked += OnNewGameClicked;
        root.Q<Button>(LoadButton).clicked += OnLoadClicked;
        root.Q<Button>(SettingsButton).clicked += OnSettingsClicked;
        root.Q<Button>(ExitButton).clicked += OnExitClicked;
    }

    private void OnNewGameClicked()
    {
        Debug.Log("New Game Clicked");
    }

    private void OnLoadClicked()
    {
        Debug.Log("Load Clicked");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("Settings Clicked");
    }

    private void OnExitClicked()
    {
        Debug.Log("Exit Clicked");
    }
}
