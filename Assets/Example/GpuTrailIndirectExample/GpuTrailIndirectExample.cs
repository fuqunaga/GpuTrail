namespace GpuTrailSystem.Example
{
    public class GpuTrailIndirectExample : GpuTrailIndirect
    {
        public GpuTrailIndirectSampleParticle _particle;

        protected override void Awake()
        {
            base.Awake();
            _particle.Init();
            gpuTrail.trailNum = _particle._particleNum;
        }



        protected override bool UpdateInputBuffer()
        {
            _particle.UpdateInputBuffer(_inputBuffer);
            return true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _particle.ReleaseBuffer();
        }
    }

}