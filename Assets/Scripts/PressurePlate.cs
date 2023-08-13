using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField] private GameObject puzzleObject; // Reference to the puzzle object that will be activated/deactivated
    [SerializeField] private string activationFunctionName = "PuzzleActivate"; // Function to call on the puzzle object
    [SerializeField] private bool requiresContinuousActivation = false; // Determines if the pressure plate requires continuous activation
    [SerializeField] private float sinkAmount = 0.2f; // Amount to sink down when activated

    private bool isActivated = false;
    private bool isCatOnPlate = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isActivated && !isCatOnPlate && other.CompareTag("Player"))
        {
            ActivatePlate();
        }
        isCatOnPlate = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isCatOnPlate = false;
        if (requiresContinuousActivation && isActivated && !IsAnyCatOnPlate())
        {
            DeactivatePlate();
        }
    }

    private bool IsAnyCatOnPlate()
    {
        PlayerController[] cats = FindObjectsOfType<PlayerController>();
        foreach (PlayerController cat in cats)
        {
            if (cat.IsOnPressurePlate(this))
            {
                return true;
            }
        }
        return false;
    }

    private void ActivatePlate()
    {
        isActivated = true;
        if (puzzleObject != null)
        {
            SendMessageToPuzzleObject(activationFunctionName);
        }
        StartCoroutine(Sink());
        Debug.Log("Activate");
    }

    private void DeactivatePlate()
    {
        isActivated = false;
        if (puzzleObject != null)
        {
            SendMessageToPuzzleObject("PuzzleDeactivate");
        }
        StartCoroutine(Rise());
        Debug.Log("Deactivate");
    }

    private void SendMessageToPuzzleObject(string functionName)
    {
        puzzleObject.SendMessage(functionName, SendMessageOptions.DontRequireReceiver);
    }

    IEnumerator Sink()
    {
        while (transform.GetChild(0).transform.localPosition.y > -sinkAmount)
        {
            transform.GetChild(0).transform.localPosition = Vector3.Lerp(transform.GetChild(0).transform.localPosition, Vector3.down * sinkAmount, Time.deltaTime * 1f);
            yield return null;
        }
    }

    IEnumerator Rise()
    {
        while (transform.GetChild(0).transform.localPosition.y < 0)
        {
            transform.GetChild(0).transform.localPosition = Vector3.Lerp(transform.GetChild(0).transform.localPosition, new Vector3(0, sinkAmount, 0), Time.deltaTime * 1f);
            yield return null;
        }
    }
}
