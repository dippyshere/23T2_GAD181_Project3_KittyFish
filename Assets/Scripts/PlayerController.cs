using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private string controlScheme = "KeyboardLeft";
    [SerializeField] private string fishTag = "Fish";
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
    private CapsuleCollider capsuleCollider => GetComponent<CapsuleCollider>();
    private bool canCatchFish = false;
    private static List<GameObject> fishToCatch = new List<GameObject>();
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
            for (int i = 0; i < fishToCatch.Count; i++)
            {
                FishController fishController = fishToCatch[i].GetComponent<FishController>();
                if (fishController != null)
                {
                    fishController.CatchFish();
                    fishCount++;
                    UpdateFishText();
                }
                fishToCatch.Remove(fishToCatch[i]);
            }
            fishToCatch.Clear();
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
        rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, new Vector3(movement.x, rigidBody.velocity.y, movement.z), Time.deltaTime * 17f);

        if (jump && canJump && !jumped)
        {
            //rigidBody.velocity = new Vector3(rigidBody.velocity.x, jumpHeight, rigidBody.velocity.z);
            rigidBody.velocity = Vector3.Lerp(rigidBody.velocity, new Vector3(rigidBody.velocity.x, jumpHeight, rigidBody.velocity.z), Time.deltaTime * 17f);
            jumped = true;
        }
        // ChatGPT
        // Check if the cat is offscreen
        if (!IsCatOnScreen())
        {
            // Calculate the position of the cat in screen space
            Vector3 catPositionInWorld = gameObject.transform.position;

            Vector3 catPositionInScreen = Camera.main.WorldToScreenPoint(catPositionInWorld);

            if (Vector3.Dot(catPositionInWorld - Camera.main.transform.position, Camera.main.transform.forward) < 0)
            {
                catPositionInWorld.z = Camera.main.transform.position.z + 0.01f;
                catPositionInScreen = Camera.main.WorldToScreenPoint(catPositionInWorld);
            }

            float offsetX = 150f * Screen.width / 1280;
            float offsetY = 150f * Screen.height / 720;

            // Clamp the circular UI element position to stay within the screen bounds
            Vector3 clampedPosition = new Vector3(
                Mathf.Clamp(catPositionInScreen.x, offscreenUI.GetComponent<RectTransform>().rect.width / 2f + offsetX, Screen.width - offscreenUI.GetComponent<RectTransform>().rect.width / 2f - offsetX),
                Mathf.Clamp(catPositionInScreen.y, offscreenUI.GetComponent<RectTransform>().rect.height / 2f + offsetY, Screen.height - offscreenUI.GetComponent<RectTransform>().rect.height / 2f - offsetY),
                catPositionInScreen.z
            );
            if (offscreenUI.activeSelf)
            {
                offscreenUI.GetComponent<RectTransform>().position = Vector3.Lerp(offscreenUI.GetComponent<RectTransform>().position, clampedPosition, Time.deltaTime * 8f);
            }
            else
            {
                offscreenUI.GetComponent<RectTransform>().position = clampedPosition;
            }

            //KeepFullyOnScreen(offscreenUI, catPositionInScreen);

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
        Vector3 raycastOrigin = transform.position + Vector3.up * 0.1f;

        // Perform raycast checks from different points
        Vector3[] raycastOrigins = new Vector3[]
        {
        raycastOrigin,                                          // Middle
        raycastOrigin + Vector3.forward * capsuleCollider.radius,    // Front
        raycastOrigin - Vector3.forward * capsuleCollider.radius,    // Back
        raycastOrigin + Vector3.left * capsuleCollider.radius,       // Left
        raycastOrigin + Vector3.right * capsuleCollider.radius,      // Right
        raycastOrigin + Vector3.forward * capsuleCollider.radius + Vector3.left * capsuleCollider.radius,  // Front-Left
        raycastOrigin + Vector3.forward * capsuleCollider.radius + Vector3.right * capsuleCollider.radius, // Front-Right
        raycastOrigin - Vector3.forward * capsuleCollider.radius + Vector3.left * capsuleCollider.radius,  // Back-Left
        raycastOrigin - Vector3.forward * capsuleCollider.radius + Vector3.right * capsuleCollider.radius, // Back-Right
        };

        foreach (Vector3 origin in raycastOrigins)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, raycastDistance))
            {
                // Adjust this check to include a small tolerance to avoid false negatives
                if (hit.distance <= raycastDistance + 0.05f)
                {
                    return true;
                }
            }
        }

        return false;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(fishTag))
        {
            canCatchFish = true;
            fishToCatch.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(fishTag))
        {
            fishToCatch.Remove(other.gameObject);
            if (fishToCatch.Count == 0)
            {
                canCatchFish = false;
            }
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
        return catPositionInScreen.x >= 0 + 50f * Screen.width / 1280 && catPositionInScreen.x <= Screen.width - 50f * Screen.width / 1280 &&
               catPositionInScreen.y >= 0 + 50f * Screen.height / 720 && catPositionInScreen.y <= Screen.height -50f * Screen.height / 720;
    }

    private void OnDrawGizmos()
    {
        float raycastDistance = 0.3f;
        Vector3 raycastOrigin = transform.position + Vector3.up * 0.1f;

        // Perform raycast checks from different points
        Vector3[] raycastOrigins = new Vector3[]
        {
        raycastOrigin,                                          // Middle
        raycastOrigin + Vector3.forward * capsuleCollider.radius,    // Front
        raycastOrigin - Vector3.forward * capsuleCollider.radius,    // Back
        raycastOrigin + Vector3.left * capsuleCollider.radius,       // Left
        raycastOrigin + Vector3.right * capsuleCollider.radius,      // Right
        raycastOrigin + Vector3.forward * capsuleCollider.radius + Vector3.left * capsuleCollider.radius,  // Front-Left
        raycastOrigin + Vector3.forward * capsuleCollider.radius + Vector3.right * capsuleCollider.radius, // Front-Right
        raycastOrigin - Vector3.forward * capsuleCollider.radius + Vector3.left * capsuleCollider.radius,  // Back-Left
        raycastOrigin - Vector3.forward * capsuleCollider.radius + Vector3.right * capsuleCollider.radius, // Back-Right
        };

        Gizmos.color = Color.green;

        foreach (Vector3 origin in raycastOrigins)
        {
            Gizmos.DrawLine(origin, origin + Vector3.down * raycastDistance);
        }
    }



    //private void KeepFullyOnScreen(GameObject gameObject, Vector3 vector3)
    //{
    //    RectTransform canvas = gameObject.transform.parent.GetComponent<RectTransform>();
    //    RectTransform rect = gameObject.GetComponent<RectTransform>();

    //    Vector2 sizeDelta = rect.sizeDelta * transform.localScale;
    //    Vector2 anchorOffset = canvas.sizeDelta * (rect.anchorMin - Vector2.one / 2);

    //    Vector2 maxPivotOffset = sizeDelta * (rect.pivot - (Vector2.one / 2) * 2);
    //    Vector2 minPivotOffset = sizeDelta * ((Vector2.one / 2) * 2 - rect.pivot);

    //    float minX = (canvas.sizeDelta.x) * -0.5f - anchorOffset.x - minPivotOffset.x + sizeDelta.x;
    //    float maxX = (canvas.sizeDelta.x) * 0.5f - anchorOffset.x + maxPivotOffset.x;
    //    float minY = (canvas.sizeDelta.y) * -0.5f - anchorOffset.y - minPivotOffset.y + sizeDelta.y;
    //    float maxY = (canvas.sizeDelta.y) * 0.5f - anchorOffset.y + maxPivotOffset.y;

    //    vector3.x = Mathf.Clamp(vector3.x, minX, maxX);
    //    vector3.y = Mathf.Clamp(vector3.y, minY, maxY);

    //    rect.anchoredPosition = new Vector2(vector3.x, vector3.y);
    //}
}
