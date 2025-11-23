using UnityEngine;
using TheFoundersPleas.InputSystem;
using TheFoundersPleas.World;

/// <summary>
/// Component that controls the singleton camera that navigates the hex map.
/// </summary>
public class HexMapCamera : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform _swivel;
    [SerializeField] private Transform _stick;

    [Header("Configuration")]
	[SerializeField] private float _stickMinZoom = -500f; 
	[SerializeField] private float _stickMaxZoom = -45f;
	[SerializeField] private float _swivelMinZoom = 90f;
	[SerializeField] private float _swivelMaxZoom = 45f; 
	[SerializeField] private float _zoomSpeed = 50f;
	[SerializeField] private float _moveSpeedMinZoom = 750f; 
	[SerializeField] private float _moveSpeedMaxZoom = 100f;
	[SerializeField] private float _rotationSpeed = 100f;

    /// <summary>
    /// Whether the singleton camera controls are locked.
    /// </summary>
    public bool Locked
    {
        set => enabled = !value;
		get => enabled;
    }

    private InputProvider _inputProvider;
    private HexGrid _hexGrid;

	private Vector2 _moveDirection;
	private float _zoomDirection;

	private bool _isRotation;
	private float _rotationDirection;

	private float _currentZoom = 1f;
	private float _currentRotation;

	public void Initialize(HexGrid hexGrid, InputProvider inputProvider)
	{
        _hexGrid = hexGrid;
		_inputProvider = inputProvider;

        _inputProvider.Moved += OnMove;
        _inputProvider.Zoomed += OnZoom;
        _inputProvider.Rotated += OnRotate;
        _inputProvider.ToggleRotationPerformed += OnToggleRotation;
        _inputProvider.ToggleRotationCancelled += OnCancelRotation;
    }

    private void OnDestroy()
    {
        _inputProvider.Moved -= OnMove;
        _inputProvider.Zoomed -= OnZoom;
        _inputProvider.Rotated -= OnRotate;
        _inputProvider.ToggleRotationPerformed -= OnToggleRotation;
        _inputProvider.ToggleRotationCancelled -= OnCancelRotation;
    }

    /// <summary>
    /// Validate the position of the singleton camera.
    /// </summary>
    public void ValidatePosition() => AdjustPosition(0f, 0f);
    private void OnMove(Vector2 direction) => _moveDirection = direction;
    private void OnZoom(float direction) => _zoomDirection = direction;
    private void OnRotate(float direction) => _rotationDirection = direction;
    private void OnToggleRotation() => _isRotation = true;
    private void OnCancelRotation() => _isRotation = false;

    private void LateUpdate()
	{
		if (_zoomDirection != 0f)
		{
			AdjustZoom(_zoomDirection);
		}

		if (_rotationDirection != 0f && _isRotation)
		{
			AdjustRotation(_rotationDirection);
		}

		if (_moveDirection.sqrMagnitude > 0)
		{
			AdjustPosition(_moveDirection.x, _moveDirection.y);
		}
	}

	private void AdjustZoom(float delta)
	{
		_currentZoom = Mathf.Clamp01(_currentZoom + delta * _zoomSpeed * Time.deltaTime);

		float distance = Mathf.Lerp(_stickMinZoom, _stickMaxZoom, _currentZoom);
		_stick.localPosition = new Vector3(0f, 0f, distance);

		float angle = Mathf.Lerp(_swivelMinZoom, _swivelMaxZoom, _currentZoom);
		_swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

    private void AdjustRotation (float delta)
	{
		_currentRotation += delta * _rotationSpeed * Time.deltaTime;
		if (_currentRotation < 0f)
		{
			_currentRotation += 360f;
		}
		else if (_currentRotation >= 360f)
		{
			_currentRotation -= 360f;
		}
		transform.localRotation = Quaternion.Euler(0f, _currentRotation, 0f);
	}

    private void AdjustPosition(float xDelta, float zDelta)
	{
		Vector3 direction = transform.localRotation 
			* new Vector3(xDelta, 0f, zDelta).normalized;

		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		float distance = Mathf.Lerp(_moveSpeedMinZoom, _moveSpeedMaxZoom, _currentZoom) 
			* damping * Time.deltaTime;

		Vector3 position = transform.localPosition;
		position += direction * distance;

		transform.localPosition = _hexGrid.Wrapping ? 
			WrapPosition(position) : 
			ClampPosition(position);
	}

    private Vector3 ClampPosition(Vector3 position)
	{
		float xMax = (_hexGrid.CellCountX - 0.5f) * HexMetrics.InnerDiameter;
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		float zMax = (_hexGrid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		return position;
	}

    private Vector3 WrapPosition(Vector3 position)
	{
		float width = _hexGrid.CellCountX * HexMetrics.InnerDiameter;
		while (position.x < 0f)
		{
			position.x += width;
		}
		while (position.x > width)
		{
			position.x -= width;
		}

		float zMax = (_hexGrid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		_hexGrid.CenterMap(position.x);
		return position;
	}
}
