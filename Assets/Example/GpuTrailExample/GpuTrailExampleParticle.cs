using System.Runtime.InteropServices;
using UnityEngine;

namespace GpuTrailSystem.Example
{

    [System.Serializable]
    public class GpuTrailExampleParticle
    {
        struct Particle
        {
            public Vector3 velocity;
            public Vector3 pos;
        }

        GraphicsBuffer particleBuffer;


        public ComputeShader particleCS;
        public int particleNum = 1000;
        public float startSpeed = 1f;
        public float forceRate = 0.01f;
        public float damping = 0.99f;
        public float gravity = 0.01f;
        public Vector3 bounds = Vector3.one * 10f;

        public void Init()
        {
            particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleNum, Marshal.SizeOf<Particle>());

            var particles = new Particle[particleBuffer.count];
            for (var i = 0; i < particles.Length; ++i)
            {
                particles[i] = new Particle
                {
                    pos = Random.insideUnitSphere * bounds.y * 0.1f,
                    velocity = Vector3.Scale(Random.insideUnitSphere, Vector3.up * startSpeed)
                };
            }

            particleBuffer.SetData(particles);
        }

        public void UpdateInputBuffer(GraphicsBuffer inputBuffer)
        {
            particleCS.SetFloat("_Time", Time.time);
            particleCS.SetFloat("_ForceRate", forceRate);
            particleCS.SetFloat("_Damping", damping);
            particleCS.SetFloat("_Gravity", gravity);
            particleCS.SetFloats("_Bounds", bounds.x, bounds.y, bounds.z);


            var kernel = particleCS.FindKernel("CSMain");
            particleCS.SetBuffer(kernel, "_ParticleBuffer", particleBuffer);
            particleCS.SetBuffer(kernel, "_InputBuffer_Pos", inputBuffer);

            ComputeShaderUtility.Dispatch(particleCS, kernel, particleBuffer.count);
        }

        public void ReleaseBuffer()
        {
            if (particleBuffer != null) particleBuffer.Release();
        }


        Particle[] particles;
        public void DrawGizmos()
        {
            if (particles == null) particles = new Particle[particleBuffer.count];

            particleBuffer.GetData(particles);

            const float radius = 0.1f;
            Gizmos.color = Color.red;
            for(var i=0; i<particles.Length; ++i)
            {
                Gizmos.DrawWireSphere(particles[i].pos, radius);
            }
        }
    }
}