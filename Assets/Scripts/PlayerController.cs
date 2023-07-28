using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private string controlScheme = "KeyboardLeft";

    private bool isWalking;
    private bool isGrounded;
    private bool jumped = false;
    private float lastTimeGrounded = 0f;
    private Vector3 velocity;
    private Vector2 movementInput = Vector2.zero;
    private PlayerInput playerInput => GetComponent<PlayerInput>();
    private CharacterController controller => GetComponent<CharacterController>();
    private Animator animator => GetComponentInChildren<Animator>();

    private void Start()
    {
        //Time.timeScale = 0.2f;
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
            jumped = true;
        }
        else if (context.canceled)
        {
            jumped = false;
        }
    }

    private void Update()
    {
        float horizontalInput = movementInput.x;
        float verticalInput = movementInput.y;

        isGrounded = controller.isGrounded;
        if (isGrounded)
        {
            lastTimeGrounded = Time.time;
        }

        bool canJump = Time.time - lastTimeGrounded <= coyoteTime;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f;
        }

        isWalking = (horizontalInput != 0f || verticalInput != 0f);
        animator.SetBool("isWalking", isWalking);
        animator.SetFloat("WalkSpeedMultiplier", Mathf.Clamp(Mathf.Sqrt(horizontalInput * horizontalInput + verticalInput * verticalInput) * walkSpeed * 1.3f, 0, walkSpeed * 1.3f));

        if (isWalking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(horizontalInput, 0f, verticalInput));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 14f);
        }

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * walkSpeed * Time.deltaTime;
        controller.Move(Vector3.ClampMagnitude(movement, walkSpeed * Time.deltaTime));

        if (jumped && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -4f * gravity);
            jumped = false;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
