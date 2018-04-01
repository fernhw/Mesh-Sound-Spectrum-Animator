using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Literally only to rotate stuff
/// </summary>
public class Rotator : MonoBehaviour {
    public Vector3 angle;
    public float speed;
	// Update is called once per frame
	void Update () {
        transform.Rotate(angle,Time.deltaTime*speed);
	}
}
