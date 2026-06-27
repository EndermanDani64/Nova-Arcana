using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

public class PlayerMovment : MonoBehaviour
{
    [Header("Movment")]
    public float movmentSpeed = 8f;
    public float groundDrag = 6f;

    [Header("Ground check")]
    public float playerHeight = 2f;
    [SerializeField] private LayerMask ground;
    bool _isGrounded;

    float _horizontalInput;
    float _verticalInput;

    Vector3 _moveDir;
    Rigidbody rigidBody;

    // sound stuff

    List<string> footstepParameterNames;
    bool _canPlayFootstep = true;

    // unity methods

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;

        footstepParameterNames = new() { "Footstep_1", "Footstep_2", "Footstep_3", "Footstep_4", "Footstep_5" };

        playerRef = gameObject.GetComponent<Player>();
    }

    private void Update()
    {
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * .5f + .2f, ground);

        GetAndUpdateInputValues();

        if (_isGrounded) rigidBody.linearDamping = groundDrag;
        else rigidBody.linearDamping = 0;

        Debug.Log($"rigidBody.linearVelocity: {rigidBody.linearVelocity}");
        if (_canPlayFootstep && _isGrounded && (Mathf.Abs(rigidBody.linearVelocity.x) > 2.6 || Mathf.Abs(rigidBody.linearVelocity.z) > 2.6))
            StartCoroutine(PlayFootstep());

        CheckRunning();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    // my methods

    private void MovePlayer()
    {
        //if (!_isGrounded) return;

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

    private IEnumerator PlayFootstep()
    {
        _canPlayFootstep = false;

        footstepsEmmiter.Play();

        float waitTime = .55f;
        if (Input.GetKey(KeyCode.LeftShift)) waitTime -= .15f;

        yield return new WaitForSeconds(waitTime);

        _canPlayFootstep = true;
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

    [Header("References")]

    [SerializeField] private Transform orientation;
    [SerializeField] private Player playerRef;
    [SerializeField] private FMODUnity.StudioEventEmitter footstepsEmmiter;
}
