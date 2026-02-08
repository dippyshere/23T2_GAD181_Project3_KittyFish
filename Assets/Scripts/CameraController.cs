using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player1Transform; // Reference to the first player's transform
    [SerializeField] private Transform player2Transform; // Reference to the second player's transform
    [SerializeField] public float followSpeed = 5f;     // Adjust this value to control camera follow speed
    [SerializeField] private float maxZoomOutDistance = 10f; // Adjust this value to control the maximum zoom-out distance
    [SerializeField] private float minZoomOutDistance = 4f;
    [SerializeField] private bool useRailSystem = true;

    // Define the rail points to limit camera movement
    [SerializeField] private Vector3 railStartPoint;
    [SerializeField] private Vector3 railEndPoint;

    private float initialCameraX;

    public bool trackCats = true;
    public Vector3 positionOverride;
    public Vector3 rotationOverride;
    public float fovOverride = 60f;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (trackCats)
        {
            initialCameraX = mainCamera.transform.position.x;
            Vector3 midpoint = (player1Transform.position + player2Transform.position) * 0.5f;
            float distanceFromMidpoint = Mathf.Abs(midpoint.x - initialCameraX);
            float distance = Vector3.Distance(player1Transform.position, player2Transform.position) + distanceFromMidpoint;
            float targetSize = distance * 1.1f;
            targetSize = Mathf.Clamp(targetSize, minZoomOutDistance, maxZoomOutDistance);
            Vector3 desiredCameraPosition = midpoint;
            desiredCameraPosition -= mainCamera.transform.forward * (targetSize * 0.7f);
            desiredCameraPosition.y += targetSize * 0.35f;
            desiredCameraPosition.z -= targetSize * 0.25f;
            desiredCameraPosition.x = Mathf.Clamp(desiredCameraPosition.x, railStartPoint.x, railEndPoint.x);
            desiredCameraPosition.z = Mathf.Clamp(desiredCameraPosition.z, railStartPoint.z, railEndPoint.z);
            mainCamera.transform.position = desiredCameraPosition;
        }
        else
        {
            mainCamera.transform.position = positionOverride;
            mainCamera.transform.rotation = Quaternion.Euler(rotationOverride);
            mainCamera.fieldOfView = fovOverride;
        }
    }

    private void Update()
    {
        if (useRailSystem)
        {
            if (trackCats)
            {
                // Calculate the midpoint between the two players
                Vector3 midpoint = (player1Transform.position + player2Transform.position) * 0.5f;

                float distanceFromMidpoint = Mathf.Abs(midpoint.x - initialCameraX);

                // Calculate the distance between the two players
                float distance = Vector3.Distance(player1Transform.position, player2Transform.position) + distanceFromMidpoint;

                // Calculate the desired zoom level based on the distance between players
                float targetSize = distance * 1.1f;

                // Clamp the desired orthographic size to a maximum value
                targetSize = Mathf.Clamp(targetSize, minZoomOutDistance, maxZoomOutDistance);

                // Calculate the desired camera position based on the midpoint and the rail limits
                Vector3 desiredCameraPosition = midpoint;
                desiredCameraPosition -= mainCamera.transform.forward * (targetSize * 0.7f); // Adjust the multiplier as needed to set the camera distance behind the midpoint
                desiredCameraPosition.y += targetSize * 0.35f;
                desiredCameraPosition.z -= targetSize * 0.25f;

                // Limit the camera movement along the rail points
                desiredCameraPosition.x = Mathf.Clamp(desiredCameraPosition.x, railStartPoint.x, railEndPoint.x);
                desiredCameraPosition.z = Mathf.Clamp(desiredCameraPosition.z, railStartPoint.z, railEndPoint.z);

                // Smoothly move the camera to the desired position
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredCameraPosition, Time.deltaTime * followSpeed);
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.Euler(26, 0, 0), Time.deltaTime * followSpeed);

                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, (targetSize / 4f) - 1f + 60f, Time.deltaTime * followSpeed);
            }
            else
            {
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, positionOverride, Time.deltaTime * followSpeed);
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.Euler(rotationOverride), Time.deltaTime * followSpeed);
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, fovOverride, Time.deltaTime * followSpeed);
            }
        }
        else
        {
            if (trackCats)
            {
                // Calculate the midpoint between the two players
                Vector3 midpoint = (player1Transform.position + player2Transform.position) * 0.5f;

                // Calculate the direction vector between the midpoint and the average cat position
                Vector3 directionToCats = (player1Transform.position + player2Transform.position) * 0.5f - midpoint;

                // Calculate the distance from the midpoint to the cats
                float distanceToCats = directionToCats.magnitude;

                // Calculate the desired camera position based on the direction and distance to the cats
                Vector3 desiredCameraPosition = midpoint - directionToCats.normalized * distanceToCats;
                desiredCameraPosition.y += 2.538863f;  // Y offset
                desiredCameraPosition.z -= 3.557927f; // Z offset

                // Calculate the look rotation towards the midpoint
                Quaternion desiredRotation = Quaternion.LookRotation(midpoint - mainCamera.transform.position);

                // Smoothly move the camera to the desired position
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredCameraPosition, Time.deltaTime * followSpeed);

                // Smoothly rotate the camera towards the desired rotation
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, desiredRotation, Time.deltaTime * followSpeed);

                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 60f, Time.deltaTime * followSpeed);
            }
            else
            {
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, positionOverride, Time.deltaTime * followSpeed);
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, Quaternion.Euler(rotationOverride), Time.deltaTime * followSpeed);
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, fovOverride, Time.deltaTime * followSpeed);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw Gizmos to visualize the rail start and end points
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(railStartPoint, 0.5f);
        Gizmos.DrawSphere(railEndPoint, 0.5f);
        Gizmos.DrawLine(railStartPoint, railEndPoint);

        //if (player1Transform != null && player2Transform != null)
        //{
        //    Gizmos.DrawSphere((player1Transform.position + player2Transform.position) * 0.5f, 0.5f);
        //}
    }
}
