namespace GpuTrailSystem.Example
{
    public class GpuTrailExample : GpuTrailAppendNode
    {
        public GpuTrailExampleParticle particle;
        public bool particleGizmosEnable;

        protected override void Start()
        {
            particle.Init();
            gpuTrail.trailNum = particle.particleNum;
            gpuTrail.Init();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            particle.ReleaseBuffer();
        }


        protected override bool UpdateInputBuffer()
        {
            particle.UpdateInputBuffer(gpuTrail.inputBuffer_Pos);
            return true;
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