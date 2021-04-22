namespace GpuTrailSystem
{
    public interface IGpuTrailAppendNode
    {
        public GpuTrail GpuTrail { get; }
        public void AppendNode();
    }
}