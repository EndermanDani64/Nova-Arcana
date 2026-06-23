using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Mouse sensitivity")]
    public float sensitivityX;
    public float sensitivityY;

    [Header("References")]
    [SerializeField] private Transform _orientation;

    float _xRotation;
    float _yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Mathf.Clamp(Input.GetAxisRaw("Mouse X") * sensitivityX * Time.deltaTime, -5f, 5f);
        float mouseY = Mathf.Clamp(Input.GetAxisRaw("Mouse Y") * sensitivityY * Time.deltaTime, -5f, 5f);

        _yRotation += mouseX;
        _xRotation -= mouseY;

        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        // rotation
        transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, _yRotation, 0);
    }
}