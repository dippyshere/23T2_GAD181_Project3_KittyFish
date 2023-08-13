using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private float openHeight = 2.0f;
    [SerializeField] private float moveSpeed = 2.0f;

    private bool isOpen = false;
    //private bool interruped = false;

    public void PuzzleActivate()
    {
        isOpen = true;
        StartCoroutine(OpenDoor());
        //Debug.Log("Activate door");
    }

    public void PuzzleDeactivate()
    {
        isOpen = false;
        StartCoroutine(CloseDoor());
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        if (!isOpen)
    //        {
    //            isOpen = true;
    //            interruped = true;
    //        }
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        if (isOpen && interruped)
    //        {
    //            PuzzleActivate();
    //            interruped = false;
    //        }
    //    }
    //}

    //public void Interrupt()
    //{
    //    if (!isOpen)
    //    {
    //        isOpen = true;
    //        interruped = true;
    //    }
    //}

    IEnumerator OpenDoor()
    {
        if (openHeight >= 0)
        {
            while (transform.GetChild(0).transform.localPosition.y < openHeight)
            {
                if (!isOpen)
                {
                    yield break;
                }
                transform.GetChild(0).transform.localPosition = Vector3.Lerp(transform.GetChild(0).transform.localPosition, new Vector3(transform.GetChild(0).transform.localPosition.x, openHeight, transform.GetChild(0).transform.localPosition.z), Time.deltaTime * moveSpeed);
                yield return null;
            }
        }
        else
        {
            while (transform.GetChild(0).transform.localPosition.y > openHeight)
            {
                if (!isOpen)
                {
                    yield break;
                }
                transform.GetChild(0).transform.localPosition = Vector3.Lerp(transform.GetChild(0).transform.localPosition, new Vector3(transform.GetChild(0).transform.localPosition.x, openHeight, transform.GetChild(0).transform.localPosition.z), Time.deltaTime * moveSpeed);
                yield return null;
            }
        }
    }

    IEnumerator CloseDoor()
    {
        if (openHeight >= 0)
        while (transform.GetChild(0).transform.localPosition.y > 0)
        {
            if (isOpen)
            {
                yield break;
            }
            transform.GetChild(0).transform.localPosition = Vector3.Lerp(transform.GetChild(0).transform.localPosition, new Vector3(transform.GetChild(0).transform.localPosition.x, 0, transform.GetChild(0).transform.localPosition.z), Time.deltaTime * moveSpeed);
            yield return null;
        }
        else
        {
            while (transform.GetChild(0).transform.localPosition.y < 0)
            {
                if (isOpen)
                {
                    yield break;
                }
                transform.GetChild(0).transform.localPosition = Vector3.Lerp(transform.GetChild(0).transform.localPosition, new Vector3(transform.GetChild(0).transform.localPosition.x, 0, transform.GetChild(0).transform.localPosition.z), Time.deltaTime * moveSpeed);
                yield return null;
            }
        }
    }
}
