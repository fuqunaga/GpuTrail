using UnityEngine;
using System.Collections;

public class AutoRotate : MonoBehaviour {

    public Vector3 rotSpeed;


	void Update () {

        transform.Rotate(rotSpeed * Time.deltaTime);
	
	}
}
