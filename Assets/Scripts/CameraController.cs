using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player1Transform; // Reference to the first player's transform
    [SerializeField] private Transform player2Transform; // Reference to the second player's transform
    [SerializeField] private float followSpeed = 5f;     // Adjust this value to control camera follow speed
    [SerializeField] private float maxZoomOutDistance = 10f; // Adjust this value to control the maximum zoom-out distance
    [SerializeField] private float minZoomOutDistance = 4f;

    // Define the rail points to limit camera movement
    [SerializeField] private Vector3 railStartPoint;
    [SerializeField] private Vector3 railEndPoint;

    private float initialCameraX;

    private void Start()
    {
        initialCameraX = Camera.main.transform.position.x;
        // Modified from ChatGPT
        Vector3 midpoint = (player1Transform.position + player2Transform.position) * 0.5f;
        float distanceFromMidpoint = Mathf.Abs(midpoint.x - initialCameraX);
        float distance = Vector3.Distance(player1Transform.position, player2Transform.position) + distanceFromMidpoint * 1.1f;
        float targetSize = distance * 1.1f;
        targetSize = Mathf.Clamp(targetSize, minZoomOutDistance, maxZoomOutDistance);
        Vector3 desiredCameraPosition = midpoint;
        desiredCameraPosition -= Camera.main.transform.forward * (targetSize * 0.7f);
        desiredCameraPosition.y += targetSize * 0.35f;
        desiredCameraPosition.z -= targetSize * 0.25f;
        desiredCameraPosition.x = Mathf.Clamp(desiredCameraPosition.x, railStartPoint.x, railEndPoint.x);
        desiredCameraPosition.z = Mathf.Clamp(desiredCameraPosition.z, railStartPoint.z, railEndPoint.z);
        Camera.main.transform.position = desiredCameraPosition;
    }

    private void Update()
    {
        // Calculate the midpoint between the two players
        Vector3 midpoint = (player1Transform.position + player2Transform.position) * 0.5f;

        float distanceFromMidpoint = Mathf.Abs(midpoint.x - initialCameraX);

        // Calculate the distance between the two players
        float distance = Vector3.Distance(player1Transform.position, player2Transform.position) + distanceFromMidpoint * 1.1f;

        // Calculate the desired zoom level based on the distance between players
        float targetSize = distance * 1.1f;

        // Clamp the desired orthographic size to a maximum value
        targetSize = Mathf.Clamp(targetSize, minZoomOutDistance, maxZoomOutDistance);

        // Calculate the desired camera position based on the midpoint and the rail limits
        Vector3 desiredCameraPosition = midpoint;
        desiredCameraPosition -= Camera.main.transform.forward * (targetSize * 0.7f); // Adjust the multiplier as needed to set the camera distance behind the midpoint
        desiredCameraPosition.y += targetSize * 0.35f;
        desiredCameraPosition.z -= targetSize * 0.25f;

        // Limit the camera movement along the rail points
        desiredCameraPosition.x = Mathf.Clamp(desiredCameraPosition.x, railStartPoint.x, railEndPoint.x);
        desiredCameraPosition.z = Mathf.Clamp(desiredCameraPosition.z, railStartPoint.z, railEndPoint.z);

        // Smoothly move the camera to the desired position
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, desiredCameraPosition, Time.deltaTime * followSpeed);
    }

    private void OnDrawGizmos()
    {
        // Draw Gizmos to visualize the rail start and end points
        if (railStartPoint != null && railEndPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(railStartPoint, 0.5f);
            Gizmos.DrawSphere(railEndPoint, 0.5f);
            Gizmos.DrawLine(railStartPoint, railEndPoint);
        }

        if (player1Transform != null && player2Transform != null)
        {
            Gizmos.DrawSphere((player1Transform.position + player2Transform.position) * 0.5f, 0.5f);
        }
    }
}
