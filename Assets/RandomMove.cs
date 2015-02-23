using UnityEngine;
using System.Collections;

public class RandomMove : MonoBehaviour {

    public float radius = 100f;
    public Vector3 target;

    public Vector2 speedMinMax;
    public float speed;

	void Start () {
        UpdateTarget();
	}
	
    void UpdateTarget()
    {
        target = Random.insideUnitSphere * radius;
        speed = Mathf.Lerp(speedMinMax.x, speedMinMax.y, Random.value);
    }

	void Update () {

        var pos = transform.position;

        if ( Vector3.Distance(target, pos) < speed * Time.deltaTime)
        {
            UpdateTarget();
        }

        var toTarget = target - pos;
        var dir = toTarget.normalized;

        pos += dir * speed * Time.deltaTime;

        transform.position = pos;

	}
}
