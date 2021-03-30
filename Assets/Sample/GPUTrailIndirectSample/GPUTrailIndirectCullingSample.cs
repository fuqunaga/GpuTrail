namespace GpuTrailSystem.Example
{
    public class GpuTrailIndirectCullingSample : GpuTrailIndirectCulling
    {
        public GpuTrailIndirectSampleParticle _particle;

        protected override int trailNumMax
        {
            get
            {
                return _particle._particleNum;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _particle.Init();
        }



        protected override bool UpdateInputBuffer()
        {
            _particle.UpdateInputBuffer(_inputBuffer);
            return true;
        }

        protected override void ReleaseBuffer()
        {
            base.ReleaseBuffer();

            _particle.ReleaseBuffer();
        }
    }
}