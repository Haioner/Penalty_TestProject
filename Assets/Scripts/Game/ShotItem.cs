using UnityEngine;
using UnityEngine.InputSystem;

public enum ShotHorizontalPos
{
    Left, Middle, Right
}

public enum ShotVerticalPos
{
    Top, Middle, Bottom
}

public class ShotItem : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private ShotHorizontalPos horizontalPosition;
    [SerializeField] private ShotVerticalPos verticalPosition;

    public ShotHorizontalPos HorizontalPosition => horizontalPosition;
    public ShotVerticalPos VerticalPosition => verticalPosition;

    private ShotTargetManager targetManager;
    private Camera mainCamera;
    private Collider targetCollider;

    private void Awake()
    {
        mainCamera = Camera.main;
        targetCollider = GetComponent<Collider>();
        targetManager = FindFirstObjectByType<ShotTargetManager>();
    }

    private void Update()
    {
        if (!IsInteractionEnabled())
            return;

        CheckMouseInput();
        CheckTouchInput();
    }

    private bool IsInteractionEnabled()
    {
        return targetManager != null && targetManager.CanInteract();
    }

    private void CheckMouseInput()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        CheckInputAtPosition(mousePosition);
    }

    private void CheckTouchInput()
    {
        if (Touchscreen.current == null)
            return;

        for (int i = 0; i < Touchscreen.current.touches.Count; i++)
        {
            var touch = Touchscreen.current.touches[i];
            if (touch.press.wasPressedThisFrame)
            {
                Vector2 touchPosition = touch.position.ReadValue();
                CheckInputAtPosition(touchPosition);
            }
        }
    }

    private void CheckInputAtPosition(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == targetCollider)
            {
                OnTargetClicked();
            }
        }
    }

    private void OnTargetClicked()
    {
        if (targetManager != null)
        {
            targetManager.OnTargetSelected(this);
        }
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
