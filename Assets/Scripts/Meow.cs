using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Meow : MonoBehaviour
{
    public AudioClip[] sounds;
    private AudioSource source;

    // Start is called before the first frame update
    private void Awake()
    {
        source = GetComponent<AudioSource>();   
    }

    public void OnMeow(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            source.clip = sounds[Random.Range(0, sounds.Length)];
            source.Play();
        }
    }
}
