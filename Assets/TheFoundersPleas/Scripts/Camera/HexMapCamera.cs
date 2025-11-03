using UnityEngine;

/// <summary>
/// Component that controls the singleton camera that navigates the hex map.
/// </summary>
public class HexMapCamera : MonoBehaviour
{
	[SerializeField]
	float stickMinZoom, stickMaxZoom;

	[SerializeField]
	float swivelMinZoom, swivelMaxZoom, zoomSpeed;

	[SerializeField]
	float moveSpeedMinZoom, moveSpeedMaxZoom;

	[SerializeField]
	float rotationSpeed;

	[SerializeField]
	HexGrid grid;

	[SerializeField]
	private InputProvider inputProvider;

	Transform swivel, stick;

	Vector2 moveDirection;

	float zoomDirection;

	bool isRotating;
	float rotationDirection;

	float zoom = 1f;

	float rotationAngle;

	static HexMapCamera instance;

	/// <summary>
	/// Whether the singleton camera controls are locked.
	/// </summary>
	public static bool Locked
	{
		set => instance.enabled = !value;
	}

	/// <summary>
	/// Validate the position of the singleton camera.
	/// </summary>
	public static void ValidatePosition() => instance.AdjustPosition(0f, 0f);

	void Awake()
	{
		swivel = transform.GetChild(0);
		stick = swivel.GetChild(0);
	}

	void OnEnable()
	{
		instance = this;
		ValidatePosition();

		inputProvider.Moved += OnMove;
		inputProvider.Zoomed += OnZoom;
		inputProvider.Rotated += OnRotate;
		inputProvider.ToggleRotationPerformed += OnToggleRotation;
		inputProvider.ToggleRotationCancelled += OnCancelRotation;
    }

    void OnDisable()
    {
        inputProvider.Moved -= OnMove;
        inputProvider.Zoomed -= OnZoom;
        inputProvider.Rotated -= OnRotate;
        inputProvider.ToggleRotationPerformed -= OnToggleRotation;
        inputProvider.ToggleRotationCancelled -= OnCancelRotation;
    }

    void OnMove(Vector2 direction)
	{
        moveDirection = direction;
    }

	void OnZoom(float direction)
	{
        zoomDirection = direction;
    }

	void OnRotate(float direction)
	{
        rotationDirection = direction;
    }

	void OnToggleRotation()
	{
		isRotating = true;
    }

    void OnCancelRotation()
    {
		isRotating = false;
    }

    void Update()
	{
		if (zoomDirection != 0f)
		{
			AdjustZoom(zoomDirection);
		}

		if (rotationDirection != 0f && isRotating)
		{
			AdjustRotation(rotationDirection);
		}

		if (moveDirection.sqrMagnitude > 0)
		{
			AdjustPosition(moveDirection.x, moveDirection.y);
		}
	}

	void AdjustZoom(float delta)
	{
		zoom = Mathf.Clamp01(zoom + delta * zoomSpeed * Time.deltaTime);

		float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
		stick.localPosition = new Vector3(0f, 0f, distance);

		float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
		swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	void AdjustRotation (float delta)
	{
		rotationAngle += delta * rotationSpeed * Time.deltaTime;
		if (rotationAngle < 0f)
		{
			rotationAngle += 360f;
		}
		else if (rotationAngle >= 360f)
		{
			rotationAngle -= 360f;
		}
		transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
	}

	void AdjustPosition(float xDelta, float zDelta)
	{
		Vector3 direction = transform.localRotation 
			* new Vector3(xDelta, 0f, zDelta).normalized;

		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) 
			* damping * Time.deltaTime;

		Vector3 position = transform.localPosition;
		position += direction * distance;

		transform.localPosition = grid.Wrapping ? 
			WrapPosition(position) : 
			ClampPosition(position);
	}

	Vector3 ClampPosition(Vector3 position)
	{
		float xMax = (grid.CellCountX - 0.5f) * HexMetrics.innerDiameter;
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		return position;
	}

	Vector3 WrapPosition(Vector3 position)
	{
		float width = grid.CellCountX * HexMetrics.innerDiameter;
		while (position.x < 0f)
		{
			position.x += width;
		}
		while (position.x > width)
		{
			position.x -= width;
		}

		float zMax = (grid.CellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		grid.CenterMap(position.x);
		return position;
	}
}
