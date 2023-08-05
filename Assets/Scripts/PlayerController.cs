using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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
    [SerializeField] private GameObject offscreenArrow;
    [SerializeField] private GameObject offscreenCamera;
    [SerializeField] private GameObject offscreenUI;

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

    private void FixedUpdate()
    {
        // ChatGPT
        // Check if the cat is offscreen
        if (!IsCatOnScreen())
        {
            // Calculate the position of the cat in screen space
            Vector3 catPositionInScreen = Camera.main.WorldToScreenPoint(gameObject.transform.position);

            // Set the circular UI element position to the cat's position in screen space
            // Clamp the circular UI element position to stay within the screen bounds
            Vector3 clampedPosition = new Vector3(
                Mathf.Clamp(catPositionInScreen.x, offscreenUI.GetComponent<RectTransform>().rect.width / 2f, Screen.width - offscreenUI.GetComponent<RectTransform>().rect.width / 2f),
                Mathf.Clamp(catPositionInScreen.y, offscreenUI.GetComponent<RectTransform>().rect.height / 2f, Screen.height - offscreenUI.GetComponent<RectTransform>().rect.height / 2f),
                catPositionInScreen.z
            );
            offscreenUI.GetComponent<RectTransform>().position = clampedPosition;

            // Calculate the angle between the cat and the center of the screen
            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0f) / 2f;
            float angle = Mathf.Atan2(catPositionInScreen.y - screenCenter.y, catPositionInScreen.x - screenCenter.x) * Mathf.Rad2Deg;

            // Rotate the arrow image to point towards the cat
            offscreenArrow.GetComponent<RectTransform>().rotation = Quaternion.Euler(0f, 0f, angle);

            offscreenUI.SetActive(true);
            offscreenCamera.SetActive(true);
        }
        else
        {
            offscreenCamera.SetActive(false);
            offscreenUI.SetActive(false);
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

    private bool IsCatOnScreen()
    {
        // ChatGPT
        // Check if the cat's position is within the screen boundaries
        Vector3 catPositionInScreen = Camera.main.WorldToScreenPoint(gameObject.transform.position);
        return catPositionInScreen.x >= 0 && catPositionInScreen.x <= Screen.width &&
               catPositionInScreen.y >= 0 && catPositionInScreen.y <= Screen.height;
    }
}
