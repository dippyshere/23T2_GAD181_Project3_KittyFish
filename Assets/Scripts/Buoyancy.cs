using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    public float waterLevel = 0f; // Set this to the y-coordinate of the water surface
    public float floatStrength = 1f; // Strength of the buoyancy force
    public float waterDrag = 0.5f; // Drag when the fish is in the water

    private Rigidbody rb;
    private bool isFloating = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isFloating = true;
            rb.drag = waterDrag;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isFloating = false;
            rb.drag = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (isFloating)
        {
            // Calculate the buoyancy force based on the fish's position relative to the water surface
            float distanceToWater = waterLevel - transform.position.y;
            if (distanceToWater > 0f)
            {
                float buoyancyForce = -Physics.gravity.y * distanceToWater * floatStrength;
                rb.AddForce(new Vector3(0f, buoyancyForce, 0f));
            }
        }
    }
}

