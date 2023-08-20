using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private UINotificationHandler uiNotificationHandler;
    [SerializeField] private PressurePlate pressurePlate1;
    public int orangeFish = 0;
    public int purpleFish = 0;

    public int checkpoint = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Checkpoint()
    {
        if (checkpoint < 1)
        {
            checkpoint++;
            StartCoroutine(uiNotificationHandler.ShowNotification());
            pressurePlate1.active = true;
        }
    }

    public void CheckFish()
    {
        if (purpleFish >= 2 && orangeFish >= 2)
        {
            Checkpoint();
        }
    }
}
