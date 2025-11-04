using TheFoundersPleas.InputSystem;
using TheFoundersPleas.World;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Component that manages the game UI.
/// </summary>
public class HexGamePlayer : MonoBehaviour
{
	[SerializeField] private InputProvider _inputProvider;
	[SerializeField] private HexGrid _hexGrid;

    private int _currentCellIndex;
    private HexUnit _selectedUnit;

    private Vector2 _mousePosition;
    private bool _isInteraction;
    private bool _isModifier;

    private void Awake()
    {
        _inputProvider.PointerMoved += OnPointerMove;
        _inputProvider.InteractPerformed += OnInteractPerformed;
        _inputProvider.InteractCancelled += OnInteractCanceled;
        _inputProvider.ModifierPerformed += OnModifierPerformed;
        _inputProvider.ModifierCancelled += OnModifierCanceled;
    }

    private void OnPointerMove(Vector2 mousePosition) => _mousePosition = mousePosition;
    private void OnInteractPerformed() => _isInteraction = true;
    private void OnInteractCanceled() => _isInteraction = false;
    private void OnModifierPerformed() => _isModifier = true;
    private void OnModifierCanceled() => _isModifier = false;

    /// <summary>
    /// Set whether map edit mode is active.
    /// </summary>
    /// <param name="toggle">Whether edit mode is enabled.</param>
    public void SetEditMode(bool toggle)
	{
		enabled = !toggle;
		_hexGrid.ShowUI(!toggle);
		_hexGrid.ClearPath();
		if (toggle)
		{
			Shader.EnableKeyword("_HEX_MAP_EDIT_MODE");
		}
		else
		{
			Shader.DisableKeyword("_HEX_MAP_EDIT_MODE");
		}
	}

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (_isInteraction)
            {
                if (_selectedUnit)
                {
                    if(_isModifier)
                    {
                        DoDeselection();
                    }
                    else
                    {
                        DoMove();
                    }
                }
                else
                {
                    DoSelection();
                }
            }
            else if (_selectedUnit)
            {
                DoPathfinding();
            }
        }
    }

    private void DoSelection()
    {
        _hexGrid.ClearPath();
        TryUpdateCurrentCell();

        if (_currentCellIndex >= 0)
        {
            _selectedUnit = _hexGrid.GetCell(_currentCellIndex).Unit;
        }
    }

    private void DoDeselection()
    {
        _hexGrid.ClearPath();
        _selectedUnit = null;
    }

    private void DoPathfinding()
    {
        if (TryUpdateCurrentCell())
        {
            if (_currentCellIndex >= 0 && _selectedUnit.IsValidDestination(_hexGrid.GetCell(_currentCellIndex)))
            {
                _hexGrid.FindPath(_selectedUnit.Location, _hexGrid.GetCell(_currentCellIndex), _selectedUnit);
            }
            else
            {
                _hexGrid.ClearPath();
            }
        }
    }

    private void DoMove()
    {
        if (_hexGrid.HasPath)
        {
            _selectedUnit.Travel(_hexGrid.GetPath());
            _hexGrid.ClearPath();
        }
    }

    private bool TryUpdateCurrentCell()
    {
        Ray ray = Camera.main.ScreenPointToRay(_mousePosition);
        HexCell cell = _hexGrid.GetCell(ray);

        int index = cell ? cell.Index : -1;
        if (index != _currentCellIndex)
        {
            _currentCellIndex = index;
            return true;
        }
        return false;
    }
}
