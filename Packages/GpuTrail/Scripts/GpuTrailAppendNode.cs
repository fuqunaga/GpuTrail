using UnityEngine;

namespace GpuTrailSystem
{
    public abstract class GpuTrailAppendNode : MonoBehaviour, IGpuTrailAppendNode
    {
        [SerializeField]
        protected GpuTrail gpuTrail;
        public GpuTrail GpuTrail => gpuTrail;


        #region Unity

        protected virtual void Start()
        {
            gpuTrail.Init();
        }

        protected virtual void OnDestroy()
        {
            gpuTrail?.Dispose();
        }

        #endregion


        /// <summary>
        /// return true if inputBuffer has updated.
        /// </summary>
        protected abstract bool UpdateInputBuffer();

        public void AppendNode()
        {
            var updated = UpdateInputBuffer();
            if (updated)
            {
                gpuTrail.DispatchAppendNode();
            }
        }


        #region Debug

        public bool debugDrawGizmosInputPos;
        public bool debugDrawGizmosNodePos;

        public virtual void OnDrawGizmosSelected()
        {
            if (debugDrawGizmosInputPos)
            {
                Gizmos.color = Color.red;
                gpuTrail.DrawGizmosInputPos(0.1f);
            }

            if (debugDrawGizmosNodePos)
            {
                Gizmos.color = Color.green;
                gpuTrail.DrawGizmosInputPos(0.1f);
            }
        }

        #endregion
    }
}