using UnityEngine;
using System.Collections;

public class GPUTrailSample : MonoBehaviour {

    public int FPS = 60;

	void Update () {

        if ( Application.targetFrameRate != FPS)
        {
            Application.targetFrameRate = FPS;
        }
	
	}
}
