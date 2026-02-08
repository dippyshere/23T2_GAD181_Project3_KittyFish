using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    static readonly int IsWalking = Animator.StringToHash("isWalking");
    static readonly int WalkSpeedMultiplier = Animator.StringToHash("WalkSpeedMultiplier");
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private string controlScheme = "KeyboardLeft";
    public string fishTag = "Fish";
    [SerializeField] private Animator animator;
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
    private GameManager gameManager => FindAnyObjectByType<GameManager>();
    public bool canCatchFish = false;
    public List<FishController> fishToCatch = new List<FishController>();
    private int fishCount = 0;
    private PressurePlate currentPressurePlate = null;

    public int fishTarget = 6;
    Camera mainCamera;
    RectTransform _rectTransform;
    RectTransform _rectTransform1;

    private void Start()
    {
        _rectTransform1 = offscreenUI.GetComponent<RectTransform>();
        _rectTransform = offscreenArrow.GetComponent<RectTransform>();
        playerInput.SwitchCurrentControlScheme(controlScheme, Keyboard.current);
        currentPressurePlate = null;
        StartCoroutine(ResetPressurePlate());
        mainCamera = Camera.main;
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
        if (!context.started || !canCatchFish)
        {
            return;
        }

        for (int i = 0; i < fishToCatch.Count; i++)
        {
            fishToCatch[i].CatchFish();
            fishCount++;
            UpdateFishText();
            fishToCatch.Remove(fishToCatch[i]);
        }
        fishToCatch.Clear();
        // Debug.Log("Fish Interact from " + controlScheme);
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

        isWalking = horizontalInput != 0f || verticalInput != 0f;
        animator.SetBool(IsWalking, isWalking);
        animator.SetFloat(WalkSpeedMultiplier, rigidBody.linearVelocity.magnitude * 1.3f);

        if (isWalking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(horizontalInput, 0f, verticalInput));
            rigidBody.MoveRotation(Quaternion.Slerp(rigidBody.rotation, targetRotation, Time.deltaTime * 14f));
        }

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * walkSpeed;
        rigidBody.linearVelocity = Vector3.Lerp(rigidBody.linearVelocity, new Vector3(movement.x, rigidBody.linearVelocity.y, movement.z), Time.deltaTime * 17f);

        if (jump && canJump && !jumped)
        {
            //rigidBody.velocity = new Vector3(rigidBody.velocity.x, jumpHeight, rigidBody.velocity.z);
            rigidBody.linearVelocity = Vector3.Lerp(rigidBody.linearVelocity, new Vector3(rigidBody.linearVelocity.x, jumpHeight, rigidBody.linearVelocity.z), Time.deltaTime * 17f);
            jumped = true;
        }
        // Check if the cat is offscreen
        if (!IsCatOnScreen())
        {
            // Calculate the position of the cat in screen space
            Vector3 catPositionInWorld = gameObject.transform.position;

            Vector3 catPositionInScreen = mainCamera.WorldToScreenPoint(catPositionInWorld);

            if (Vector3.Dot(catPositionInWorld - mainCamera.transform.position, mainCamera.transform.forward) < 0)
            {
                catPositionInWorld.z = mainCamera.transform.position.z + 0.01f;
                catPositionInScreen = mainCamera.WorldToScreenPoint(catPositionInWorld);
            }

            float offsetX = 150f * Screen.width / 1280;
            float offsetY = 150f * Screen.height / 720;

            // Clamp the circular UI element position to stay within the screen bounds
            Vector3 clampedPosition = new Vector3(
                Mathf.Clamp(catPositionInScreen.x, _rectTransform1.rect.width / 2f + offsetX, Screen.width - _rectTransform1.rect.width / 2f - offsetX),
                Mathf.Clamp(catPositionInScreen.y, _rectTransform1.rect.height / 2f + offsetY, Screen.height - _rectTransform1.rect.height / 2f - offsetY),
                catPositionInScreen.z
            );
            _rectTransform1.position = offscreenUI.activeSelf ? Vector3.Lerp(_rectTransform1.position, clampedPosition, Time.deltaTime * 8f) : clampedPosition;

            //KeepFullyOnScreen(offscreenUI, catPositionInScreen);

            // Calculate the angle between the cat and the center of the screen
            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0f) / 2f;
            float angle = Mathf.Atan2(catPositionInScreen.y - screenCenter.y, catPositionInScreen.x - screenCenter.x) * Mathf.Rad2Deg;

            // Rotate the arrow image to point towards the cat
            _rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);

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
        //if (other.CompareTag(fishTag))
        //{
        //    if (other.GetComponent<FishController>() != null)
        //    {
        //        canCatchFish = true;
        //        fishToCatch.Add(other.GetComponent<FishController>());
        //    }
        //}
        if (other.CompareTag("PressurePlate"))
        {
            if (Vector3.Distance(other.transform.position, transform.position) > 3f)
            {
                return;
            }
            currentPressurePlate = other.GetComponent<PressurePlate>();
            if (currentPressurePlate != null)
            {
                currentPressurePlate.OnTriggerEnter(capsuleCollider);
            }
        }
        //else if (other.CompareTag("Door"))
        //{
        //    if (other.GetComponent<DoorController>() != null)
        //    {
        //        other.GetComponent<DoorController>().Interrupt();
        //    }
        //    Debug.Log("Door");
        //}
    }

    private void OnTriggerExit(Collider other)
    {
        //if (other.CompareTag(fishTag))
        //{
        //    for (int i = 0; i < fishToCatch.Count; i++)
        //    {
        //        if (fishToCatch[i] == other.GetComponent<FishController>())
        //        {
        //            fishToCatch.RemoveAt(i);
        //        }
        //    }
        //    if (fishToCatch.Count == 0)
        //    {
        //        canCatchFish = false;
        //    }
        //}
        if (other.CompareTag("PressurePlate") && currentPressurePlate == other.GetComponent<PressurePlate>())
        {
            if (other.GetComponent<PressurePlate>() != null)
            {
                currentPressurePlate = null;
                other.GetComponent<PressurePlate>().OnTriggerExit(capsuleCollider);
            }
        }
    }

    private void UpdateFishText()
    {
        if (fishCountText != null)
        {
            fishCountText.text = fishCount.ToString() + " / " + fishTarget.ToString();
        }
        if (controlScheme == "KeyboardLeft")
        {
            gameManager.orangeFish = fishCount;
        }
        else if (controlScheme == "KeyboardRight")
        {
            gameManager.purpleFish = fishCount;
        }
        gameManager.CheckFish();
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
        // Check if the cat's position is within the screen boundaries
        Vector3 catPositionInScreen = mainCamera.WorldToScreenPoint(gameObject.transform.position);
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

    public bool IsOnPressurePlate(PressurePlate pressurePlate)
    {
        return currentPressurePlate == pressurePlate;
    }

    IEnumerator ResetPressurePlate()
    {
        yield return new WaitForSeconds(0.3f);
        currentPressurePlate = null;
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
