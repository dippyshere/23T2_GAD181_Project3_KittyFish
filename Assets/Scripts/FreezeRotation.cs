using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeRotation : MonoBehaviour
{
    private Vector3 pivotOffset;
    private Quaternion fixedRotation;

    private void Start()
    {
        // Calculate the offset from the camera to the pivot point
        pivotOffset = transform.position + transform.parent.position;

        pivotOffset.x = 0f;

        // Store the fixed rotation relative to the pivot
        fixedRotation = Quaternion.Euler(26f, 0f, 0f);
    }

    private void LateUpdate()
    {
        // Calculate the desired position of the camera based on the parent's position and the pivot offset
        Vector3 desiredPosition = transform.parent.position + pivotOffset;

        // Set the camera's position to the desired position
        transform.position = desiredPosition;

        // Apply the fixed rotation to the camera
        transform.rotation = fixedRotation;
    }
}
