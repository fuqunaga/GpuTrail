namespace GpuTrailSystem.Example
{
    public class GpuTrailExample : GpuTrailAppendNode
    {
        public GpuTrailExampleParticle particle;
        public bool particleGizmosEnable;

        protected void Start()
        {
            particle.Init();
            gpuTrail.trailNum = particle.particleNum;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            particle.ReleaseBuffer();
        }


        protected override int UpdateInputBuffer()
        {
            particle.UpdateInputBuffer(InputBufferPos);
            return 1;
        }


        public override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (particleGizmosEnable)
            {
                particle.DrawGizmos();
            }
        }
    }

}