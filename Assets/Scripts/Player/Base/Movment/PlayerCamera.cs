using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Mouse sensitivity")]
    public float sensitivityX;
    public float sensitivityY;

    [Header("References")]
    public Transform orientation;

    float _xRotation;
    float _yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX * Time.deltaTime;    
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY * Time.deltaTime;

        _yRotation += mouseX;
        _xRotation -= mouseY;

        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        // rotation
        transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, _yRotation, 0);
    }
}