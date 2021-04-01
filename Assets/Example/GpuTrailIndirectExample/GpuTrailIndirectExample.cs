using UnityEngine;

namespace GpuTrailSystem.Example
{
    public class GpuTrailIndirectExample : MonoBehaviour, IGpuTrailHolder
    {
        public ComputeShaderParticle particle;

        [SerializeField]
        protected GpuTrail gpuTrail;

        public GpuTrail GpuTrail => gpuTrail;

        void Start()
        {
            particle.Init();
            gpuTrail.trailNum = particle._particleNum;
            gpuTrail.Init();
        }


        void Update()
        {
            particle.UpdateInputBuffer(gpuTrail.inputBuffer_Pos);
            gpuTrail.DispatchAppendNode();
        }

        void OnDestroy()
        {
            particle.ReleaseBuffer();
            gpuTrail.Dispose();
        }

        void OnDrawGizmosSelected()
        {
            particle.DrawGizmos();
        }
    }

}