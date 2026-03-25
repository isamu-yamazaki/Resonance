#if !UNITY_SERVER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadBank : MonoBehaviour
{
    public AK.Wwise.Bank soundBank;
    // Start is called before the first frame update
    void Start()
    {
        soundBank.Load();
    }
}
#endif
