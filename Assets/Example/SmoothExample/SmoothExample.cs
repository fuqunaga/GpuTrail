using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GpuTrailSystem.Example
{

    public class SmoothExample : MonoBehaviour
    {
        public GameObject sphere;
        public List<Transform> path = new List<Transform>();


        void Start()
        {
            var go = Instantiate(sphere);

            StartCoroutine(MoveSequence(go));
        }

        IEnumerator MoveSequence(GameObject go)
        {
            foreach (var trans in path)
            {
                go.transform.position = trans.position;
                yield return null;
            }

            Destroy(go);
        }


        static GUIStyle style;
        void OnDrawGizmos()
        {
            if (style == null)
            {
                style = GUI.skin.label;
                style.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
            }

            for (var i = 0; i < path.Count; ++i)
            {
                var point = path[i];
                Handles.Label(point.position, i.ToString(), style);
            }
        }
    }
}