using UnityEngine;
using System.Collections;

public class GeneratePrefab : MonoBehaviour {

    public GameObject prefab;
    public int num;
    public float radius;

	void Awake () {

        for(var i=0; i<num; ++i)
        {
            var pos = Random.insideUnitSphere * radius;
            var go = Instantiate(prefab, pos, Quaternion.identity) as GameObject;
            if (go) go.transform.SetParent(transform);
        }
	
	}

}
