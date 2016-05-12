using UnityEngine;
using System.Collections;

public class GenerateSphere : MonoBehaviour {

    public GameObject prefab;
    public int num;
    public float radius;

	void Awake () {

        for(var i=0; i<num; ++i)
        {
            var pos = Random.insideUnitSphere * radius + transform.position;
            var go = Instantiate(prefab, pos, Quaternion.identity) as GameObject;
            if (go)
            {
                go.transform.SetParent(transform);
                go.GetComponent<Rigidbody>().velocity = Random.insideUnitSphere * 10f;
            }
        }
	
	}

    void Update()
    {
        if ( Input.GetMouseButton(0))
        {
            var pos = Camera.main.transform.position;
            var foward = Camera.main.transform.forward;

            var go = Instantiate(prefab, pos + foward*2f, Quaternion.identity) as GameObject;
            if ( go )
            {
                go.transform.SetParent(transform);
                go.GetComponent<Rigidbody>().velocity = Camera.main.transform.TransformVector(Quaternion.Euler(Random.Range(-30f, 10f), 0f, 0f) * Vector3.forward * Random.Range(10f, 100f));
            }
        }
    }

    public void OnGUI()
    {
        GUILayout.Label("SphereCount: " + transform.childCount);
        if ( GUILayout.Button("Clear") )
        {
            var num = transform.childCount;
            for (var i = 0; i < num; ++i)
                Destroy(transform.GetChild(i).gameObject);
        }

    }
}
