using UnityEngine;

namespace GpuTrailSystem
{
    public abstract class GpuTrailAppendNode : MonoBehaviour, IGpuTrailHolder
    {
        public GpuTrail gpuTrail;
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



        protected virtual void LateUpdate()
        {
            var updated = UpdateInputBuffer();
            if (updated)
            {
                gpuTrail.DispatchAppendNode();
            }
        }

        #endregion


        /// <summary>
        /// return true if inputBuffer has updated.
        /// </summary>
        protected abstract bool UpdateInputBuffer();
    }
}