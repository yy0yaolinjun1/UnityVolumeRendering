using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTheVolume : MonoBehaviour {
    // Use this for initialization
    void Start()
    {

    }
    public float xangle = 0.0f;
    public float yangle = 0.0f;
    public float zangle = 0.0f;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
         //   Debug.Log("fuck");
         //   this.transform.Rotate(0, 30, 0);
        }
        this.transform.Rotate(xangle,yangle, zangle);
    }
}
