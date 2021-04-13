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

        public float gizmosSize = 0.5f;
        public bool gizmosDrawInputPos;
        public bool gizmosDrawNodePos;

        public virtual void OnDrawGizmosSelected()
        {
            if (gizmosDrawInputPos)
            {
                Gizmos.color = Color.red;
                gpuTrail.DrawGizmosInputPos(gizmosSize);
            }

            if (gizmosDrawNodePos)
            {
                Gizmos.color = Color.green;
                gpuTrail.DrawGizmosNodePos(gizmosSize);
            }
        }

        #endregion
    }
}