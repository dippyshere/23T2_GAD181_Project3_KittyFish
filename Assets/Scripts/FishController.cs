using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FishController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float swimmingSpeed = 2f;
    [SerializeField] private float jumpInterval = 3f;
    [SerializeField] private Mesh[] meshes;
    [SerializeField] private Bounds pondBounds;
    [SerializeField] private GameObject interactPrompt;

    private bool isLured = false;
    private bool isJumping = false;
    private float jumpForce = 5f;
    private Rigidbody rb;
    private Vector3 targetPosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (meshes != null && meshes.Length > 0)
        {
            int randomMeshIndex = Random.Range(0, meshes.Length);
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = meshes[randomMeshIndex];
            }
        }

        SetRandomSwimTarget();
    }

    private void Update()
    {
        if (isLured && !isJumping)
        {
            // Move the fish away from the player position
            Vector3 directionFromLure = (transform.position - player.position).normalized;
            directionFromLure.y = 0f;
            rb.velocity = directionFromLure * swimmingSpeed;
        }

        if (!isJumping && !isLured)
        {
            // Swim towards the target position
            Vector3 swimDirection = (targetPosition - transform.position).normalized;
            rb.velocity = swimDirection * swimmingSpeed;

            // Check if the fish has reached the target position
            if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
            {
                // Set a new random target position for swimming
                SetRandomSwimTarget();

                // Start the jumping timer
                Invoke("Jump", jumpInterval);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLured && other.CompareTag("Lure"))
        {
            isLured = true;
        }
        if (other.CompareTag("Player"))
        {
            interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isLured && other.CompareTag("Lure"))
        {
            isLured = false;
        }
        if (other.CompareTag("Player"))
        {
            interactPrompt.SetActive(false);
        }
    }

    public void CatchFish()
    {
        // Implement the logic to catch the fish here
        // For example, you can add points, increase the player's inventory, etc.

        // After catching the fish, remove it from the scene
        Destroy(gameObject);
    }

    private void Jump()
    {
        // Jump the fish
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Reset the swimming velocity so it doesn't affect the jump
        rb.velocity = Vector3.zero;

        // Set the jumping flag to true
        isJumping = true;

        // Call StopJumping after a short delay to reset the jumping flag
        Invoke("StopJumping", 0.5f);
    }

    private void StopJumping()
    {
        // Reset the jumping flag
        isJumping = false;
    }

    private void SetRandomSwimTarget()
    {
        // Generate a random target position within the pond bounds
        float randomX = Random.Range(pondBounds.min.x, pondBounds.max.x);
        float randomZ = Random.Range(pondBounds.min.z, pondBounds.max.z);
        targetPosition = new Vector3(randomX, transform.position.y, randomZ);
    }

    private void OnDrawGizmos()
    {
        if (targetPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPosition, 0.5f);
        }
        Gizmos.DrawWireCube(pondBounds.center, pondBounds.size);
    }
}
