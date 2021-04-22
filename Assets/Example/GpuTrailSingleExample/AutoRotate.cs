using UnityEngine;

namespace GpuTrailSystem.Example
{
    public class AutoRotate : MonoBehaviour
    {
        public Vector3 rotSpeed;


        void Update()
        {
            transform.Rotate(rotSpeed * Time.deltaTime);
        }
    }
}