using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace GpuTrailSystem.Example
{

    [System.Serializable]
    public class GpuTrailIndirectExampleParticle
    {
        struct Particle
        {
            public Vector3 velocity;
            public Vector3 pos;
        }

        GraphicsBuffer _particleBuffer;


        public ComputeShader _particleCS;
        public int _particleNum = 1000;
        public float _startSpeed = 1f;
        public float _forceRate = 0.01f;
        public float _damping = 0.99f;
        public float _gravity = 0.01f;
        public Vector3 _bounds = Vector3.one * 10f;

        public void Init()
        {
            _particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _particleNum, Marshal.SizeOf<Particle>());

            var particles = new Particle[_particleBuffer.count];
            for (var i = 0; i < particles.Length; ++i)
            {
                particles[i] = new Particle
                {
                    pos = Random.insideUnitSphere * _bounds.y * 0.1f,
                    velocity = Vector3.Scale(Random.insideUnitSphere, Vector3.up * _startSpeed)
                };
            }

            _particleBuffer.SetData(particles);
        }

        public void UpdateInputBuffer(GraphicsBuffer inputBuffer)
        {
            _particleCS.SetFloat("_Time", Time.time);
            _particleCS.SetFloat("_ForceRate", _forceRate);
            _particleCS.SetFloat("_Damping", _damping);
            _particleCS.SetFloat("_Gravity", _gravity);
            _particleCS.SetFloats("_Bounds", _bounds.x, _bounds.y, _bounds.z);


            var kernel = _particleCS.FindKernel("CSMain");
            _particleCS.SetBuffer(kernel, "_ParticleBuffer", _particleBuffer);
            _particleCS.SetBuffer(kernel, "_InputBuffer_Pos", inputBuffer);

            ComputeShaderUtility.Dispatch(_particleCS, kernel, _particleBuffer.count);
        }

        public void ReleaseBuffer()
        {
            if (_particleBuffer != null) _particleBuffer.Release();
        }


        Particle[] particles;
        public void DrawGizmos()
        {
            if (particles == null) particles = new Particle[_particleBuffer.count];

            _particleBuffer.GetData(particles);

            const float radius = 0.1f;
            Gizmos.color = Color.red;
            for(var i=0; i<particles.Length; ++i)
            {
                Gizmos.DrawWireSphere(particles[i].pos, radius);
            }
        }
    }
}