using UnityEngine;

namespace GpuTrailSystem.Example
{
    public class GpuTrailIndirectExample : MonoBehaviour, IGpuTrailHolder
    {
        public GpuTrailIndirectSampleParticle particle;

        [SerializeField]
        protected GpuTrail gpuTrail;

        public GpuTrail GpuTrail => gpuTrail;

        void Awake()
        {
            particle.Init();
            gpuTrail.trailNum = particle._particleNum;
            gpuTrail.Init();
        }



        void Update()
        {
            //TODO:
            //particle.UpdateInputBuffer(_inputBuffer);
        }

        void OnDestroy()
        {
            particle.ReleaseBuffer();
            gpuTrail.Dispose();
        }
    }

}