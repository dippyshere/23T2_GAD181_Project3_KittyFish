using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLock : MonoBehaviour
{
    [SerializeField] private Vector3 cameraPosition;
    [SerializeField] private Vector3 cameraRotation;
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;
    [SerializeField] private int orangeFishTarget = 6;
    [SerializeField] private int purpleFishTarget = 6;
    [SerializeField] private bool orangeFish = false;
    [SerializeField] private bool purpleFish = false;

    private CameraController cameraController => Camera.main.GetComponent<CameraController>();
    private GameManager gameManager => FindObjectOfType<GameManager>();
    private BoxCollider boxCollider => GetComponent<BoxCollider>();
    private bool cameraLocked = false;

    private void FixedUpdate()
    {
        if (cameraLocked)
        {
            Vector3 midpoint = (player1Transform.position + player2Transform.position) * 0.5f;
            if (!boxCollider.bounds.Contains(midpoint))
            {
                cameraLocked = false;
                cameraController.trackCats = true;
                if (orangeFish)
                {
                    PlayerController playerController = player1Transform.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.HideFishUI();
                    }
                }
                if (purpleFish)
                {
                    PlayerController playerController = player2Transform.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.HideFishUI();
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Triggered");
        if (other.CompareTag("MainCamera"))
        {
            cameraLocked = true;
            cameraController.trackCats = false;
            cameraController.positionOverride = cameraPosition;
            cameraController.rotationOverride = cameraRotation;
            // Debug.Log("Camera locked");
            if (orangeFish)
            {
                PlayerController playerController = player1Transform.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.fishTarget = orangeFishTarget;
                    playerController.ShowFishUI();
                }
            }
            if (purpleFish)
            {
                PlayerController playerController = player2Transform.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.fishTarget = purpleFishTarget;
                    playerController.ShowFishUI();
                }
            }
        }
    }

    // ChatGPT
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // Draw a sphere at the locked position
        Gizmos.DrawSphere(cameraPosition, 0.2f);

        // Draw a line from the locked position to show the locked rotation
        Vector3 forwardDirection = Quaternion.Euler(cameraRotation) * Vector3.forward;
        Gizmos.DrawLine(cameraPosition, cameraPosition + forwardDirection);
    }
}
