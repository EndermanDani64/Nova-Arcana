using UnityEngine;

public class PlayerMovment : MonoBehaviour
{
    [Header("Movment")]
    public float movmentSpeed = 8f;
    public float groundDrag = 6f;

    [Header("Ground check")]
    public float playerHeight = 2f;
    [SerializeField] private LayerMask ground;
    bool _isGrounded;

    [Header("References")]
    [SerializeField] private Transform orientation;

    float _horizontalInput;
    float _verticalInput;

    Vector3 _moveDir;
    Rigidbody rigidBody;

    // unity methods

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;

        playerRef = gameObject.GetComponent<Player>();
    }

    private void Update()
    {
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * .5f + .2f, ground);

        GetAndUpdateInputValues();

        Debug.Log($"isGrounded = {_isGrounded}");

        if (_isGrounded) rigidBody.linearDamping = groundDrag;
        else rigidBody.linearDamping = 0;

        CheckRunning();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    // my methods

    private void MovePlayer()
    {
        // calc movent dir
        _moveDir = orientation.forward * _verticalInput + orientation.right * _horizontalInput;

        if (!playerRef.staminaRegen)
            rigidBody.AddForce(_moveDir.normalized * movmentSpeed * 17f, ForceMode.Force);
        else
            rigidBody.AddForce(_moveDir.normalized * movmentSpeed * 10f, ForceMode.Force);
    }

    /// <summary>
    /// If the player doesn't run, we increase the stamina, if we run, we decerease it.
    /// </summary>
    private void CheckRunning()
    {
        if (!Input.GetKey(KeyCode.LeftShift) || playerRef.stamina <= 0)
        {
            playerRef.staminaRegen = true;
            playerRef.ModifyStamina(0.01f);
        }
        else if (Input.GetKey(KeyCode.LeftShift) && playerRef.stamina > 0)
        {
            playerRef.staminaRegen = false;
            playerRef.ModifyStamina(-0.05f);
        }
    }

    // base methods

    /// <summary>
    /// Getting some important floats for the base movment.
    /// </summary>
    private void GetAndUpdateInputValues()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");
    }

    private Player playerRef;
}
