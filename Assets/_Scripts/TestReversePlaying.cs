using UnityEngine;
using System.Collections;

public class TestReversePlaying : MonoBehaviour {

    public void PlayReverse()
    {
        audio.time = audio.clip.length;
        audio.Play();
    }
}
