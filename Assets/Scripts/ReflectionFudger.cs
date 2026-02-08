using System;
using UnityEngine;
using UnityEngine.Rendering;

public class ReflectionFudger : MonoBehaviour
{
    //public Transform cameraTransform;
    //private bool trackingCamera = true;
    public Transform reflectionProbeTransform;
    public ReflectionProbe reflectionProbe;
    Vector3 initialProbeTransform;
    private Vector3 initialProbeSize;
    private Vector3 initialProbeOffset;
    public Vector3 catProbePosition;
    public Vector3 catBoxSize;
    public Vector3 catBoxCenter;

    private void Start()
    {
        if (reflectionProbe)
        {
            initialProbeTransform = reflectionProbeTransform.position;
            initialProbeSize = reflectionProbe.size;
            initialProbeOffset = reflectionProbe.center;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("MainCamera"))
        {
            return;
        }

        reflectionProbe.mode = ReflectionProbeMode.Realtime;
        reflectionProbeTransform.position = catProbePosition;
        reflectionProbe.size = catBoxSize;
        reflectionProbe.center = catBoxCenter;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("MainCamera"))
        {
            return;
        }

        reflectionProbe.mode = ReflectionProbeMode.Baked;
        reflectionProbeTransform.position = initialProbeTransform;
        reflectionProbe.size = initialProbeSize;
        reflectionProbe.center = initialProbeOffset;
    }
}
