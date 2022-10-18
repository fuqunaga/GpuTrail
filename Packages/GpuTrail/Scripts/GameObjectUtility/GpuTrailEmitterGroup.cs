using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace GpuTrailSystem
{
    public class GpuTrailEmitterGroup : GpuTrailAppendNode
    {
        #region Static

        static readonly Dictionary<string, GpuTrailEmitterGroup> GroupDic = new();

        static void RegisterGroup(GpuTrailEmitterGroup group)
        {
            GroupDic[group.groupName] = group;
        }

        public static void RegisterEmitter(string groupName, GpuTrailEmitter emitter)
        {
            if (GroupDic.TryGetValue(groupName, out var trailGroup))
            {
                trailGroup.Register(emitter);
            }
        }

        #endregion


        public string groupName;

        readonly List<GpuTrailEmitter> _emitters = new();
        readonly List<(Vector3, Vector3)> _emitterPosLogs = new();
        NativeArray<Vector3> _posArray;

        void Start()
        {
            RegisterGroup(this);
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            _posArray.Dispose();
        }


        void CheckBuffers()
        {
            if (InputBufferPos.count != BufferSize)
            {
                InitBuffers();
            }

            if (!_posArray.IsCreated || _posArray.Length != InputBufferPos.count)
            {
                if (_posArray.IsCreated) _posArray.Dispose();
                _posArray = new NativeArray<Vector3>(InputBufferPos.count, Allocator.Persistent);
            }
        }

        protected override int UpdateInputBuffer()
        {
#if UNITY_EDITOR
            if ( Mathf.Max(30f, Application.targetFrameRate) * inputCountMax > gpuTrail.life * gpuTrail.inputPerSec)
            {
                Debug.LogWarning("GpuTrail nodeBuffer will overflow. Please set gpuTrail.inputPerSec > inputCount * Application.targetFrameFate");
            }
#endif

            CheckBuffers();

            while (_emitterPosLogs.Count < _emitters.Count)
            {
                _emitterPosLogs.Add((default(Vector3), default(Vector3)));
            }


            var trailNum = gpuTrail.trailNum;

            for (var i = 0; i < _emitters.Count; ++i)
            {
                var inputIdx = 0;
                var emitter = _emitters[i];
                if (emitter != null && emitter.enabled)
                {
                    var pos = emitter.transform.position;

                    var (prev0, prev1) = _emitterPosLogs[i];
                    var hasLog = prev0 != default && prev1 != default;
                    if (hasLog)
                    {
                        for (; inputIdx < inputCountMax; ++inputIdx)
                        {
                            var t = (float)(inputIdx + 1) / inputCountMax; //0:exclude 1:include
                            _posArray[trailNum * inputIdx + i] = Spline.CatmullRom(t, prev1, prev0, pos);
                        }
                    }
                    else
                    {
                        _posArray[i] = pos;
                        inputIdx++;
                    }

                    prev1 = prev0;
                    prev0 = pos;
                    _emitterPosLogs[i] = (prev0, prev1);
                }

                // fill ignore point
                for (; inputIdx < inputCountMax; ++inputIdx)
                {
                    _posArray[trailNum * inputIdx + i] = default;
                }
            }


            InputBufferPos.SetData(_posArray);

            return _emitters.Any() ? inputCountMax : 0;
        }


        public void Register(GpuTrailEmitter emitter)
        {
            _emitters.Add(emitter);
        }
    }
}