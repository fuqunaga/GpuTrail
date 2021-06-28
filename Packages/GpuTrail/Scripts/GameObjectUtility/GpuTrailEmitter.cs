using UnityEngine;

namespace GpuTrailSystem
{

    public class GpuTrailEmitter : MonoBehaviour
    {
        public string groupName;

        void Start()
        {
            GpuTrailEmitterGroup.RegisterEmitter(groupName, transform);
        }
    }
}