namespace GpuTrailSystem.Example
{
    public class GpuTrailIndirectExample : GpuTrailIndirect
    {
        public GpuTrailIndirectSampleParticle _particle;

        public override int trailNumMax
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