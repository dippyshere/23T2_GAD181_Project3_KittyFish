using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINotificationHandler : MonoBehaviour
{
    [SerializeField] private GameObject notificationObject;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(ShowNotification());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator ShowNotification()
    {
        while (notificationObject.GetComponent<RectTransform>().anchoredPosition.y >= -64)
        {
            Vector2 anchoredPosition = notificationObject.GetComponent<RectTransform>().anchoredPosition;
            anchoredPosition.y = Mathf.Lerp(anchoredPosition.y, -64.1f, Time.deltaTime * 2f);
            notificationObject.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            yield return null;
        }

        // yield return new WaitForSeconds(0.5f);

        while (notificationObject.GetComponent<RectTransform>().anchoredPosition.y < 30)
        {
            Vector2 anchoredPosition = notificationObject.GetComponent<RectTransform>().anchoredPosition;
            anchoredPosition.y = Mathf.Lerp(anchoredPosition.y, 30.1f, Time.deltaTime * 2f);
            notificationObject.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            yield return null;
        }
    }
}
