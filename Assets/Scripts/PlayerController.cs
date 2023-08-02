using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private string controlScheme = "KeyboardLeft";
    [SerializeField] private Animator[] animators;
    [SerializeField] private GameObject fishUI;
    [SerializeField] private TextMeshProUGUI fishCountText;

    private bool isWalking;
    private bool isGrounded;
    private bool jump = false;
    private bool jumped = false;
    private float lastTimeGrounded = 0f;
    private Vector3 velocity;
    private Vector2 movementInput = Vector2.zero;
    private PlayerInput playerInput => GetComponent<PlayerInput>();
    private Rigidbody rigidBody => GetComponent<Rigidbody>();
    private bool canCatchFish = false;
    private GameObject fishToCatch;
    private int fishCount = 0;

    public int fishTarget = 6;

    private void Start()
    {
        playerInput.SwitchCurrentControlScheme(controlScheme, Keyboard.current);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jump = true;
        }
        else if (context.canceled)
        {
            jump = false;
            jumped = false;
        }
    }

    public void OnFishInteract(InputAction.CallbackContext context)
    {
        if (context.started && canCatchFish)
        {
            FishController fishController = fishToCatch.GetComponent<FishController>();
            if (fishController != null)
            {
                fishController.CatchFish();
                fishCount++;
                UpdateFishText();
            }
        }
    }

    private void Update()
    {
        float horizontalInput = movementInput.x;
        float verticalInput = movementInput.y;

        isGrounded = IsGrounded();
        if (isGrounded)
        {
            lastTimeGrounded = Time.time;
            jumped = false;
        }

        bool canJump = Time.time - lastTimeGrounded <= coyoteTime;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f;
        }

        isWalking = (horizontalInput != 0f || verticalInput != 0f);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].SetBool("isWalking", isWalking);
        }
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].SetFloat("WalkSpeedMultiplier", rigidBody.velocity.magnitude * 1.3f);
        }

        if (isWalking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(horizontalInput, 0f, verticalInput));
            rigidBody.MoveRotation(Quaternion.Slerp(rigidBody.rotation, targetRotation, Time.deltaTime * 14f));
        }

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * walkSpeed;
        rigidBody.velocity = new Vector3(movement.x, rigidBody.velocity.y, movement.z);

        if (jump && canJump && !jumped)
        {
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, jumpHeight);
            jumped = true;
        }
    }

    private bool IsGrounded()
    {
        float raycastDistance = 0.3f;
        RaycastHit hit;
        return Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Fish"))
        {
            canCatchFish = true;
            fishToCatch = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Fish"))
        {
            canCatchFish = false;
            fishToCatch = null;
        }
    }

    private void UpdateFishText()
    {
        if (fishCountText != null)
        {
            fishCountText.text = fishCount.ToString() + " / " + fishTarget.ToString();
        }
    }

    public void ShowFishUI()
    {
        if (fishUI != null)
        {
            UpdateFishText();
            fishUI.SetActive(true);
        }
    }

    public void HideFishUI()
    {
        if (fishUI != null)
        {
            fishUI.SetActive(false);
        }
    }
}
