using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private Animator animator;

    private bool isWalking;
    private CharacterController controller;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Temp
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        isWalking = (horizontalInput != 0f || verticalInput != 0f);
        animator.SetBool("isWalking", isWalking);
        animator.SetFloat("WalkSpeedMultiplier", Mathf.Clamp(Mathf.Sqrt(horizontalInput * horizontalInput + verticalInput * verticalInput) * walkSpeed * 1.3f, 0, walkSpeed * 1.3f));
        if (isWalking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(horizontalInput, 0f, verticalInput));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void FixedUpdate()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * walkSpeed;

        controller.SimpleMove(Vector3.ClampMagnitude(movement, walkSpeed));
    }
}
