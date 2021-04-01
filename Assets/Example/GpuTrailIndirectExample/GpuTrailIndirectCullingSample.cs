namespace GpuTrailSystem.Example
{
    public class GpuTrailIndirectCullingSample : GpuTrailIndirectCulling
    {
        public ComputeShaderParticle _particle;


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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _particle.ReleaseBuffer();
        }
    }
}