using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;


namespace GpuTrailSystem
{
    public class GpuTrailEmitterGroup : GpuTrailAppendNode
    {
        #region Static

        static readonly Dictionary<string, GpuTrailEmitterGroup> groupDic = new Dictionary<string, GpuTrailEmitterGroup>();

        static void RegisterGroup(GpuTrailEmitterGroup group)
        {
            groupDic[group.groupName] = group;
        }

        public static void RegisterEmitter(string groupName, GpuTrailEmitter emitter)
        {
            if (groupDic.TryGetValue(groupName, out var trailGroup))
            {
                trailGroup.Register(emitter);
            }
        }

        #endregion


        public string groupName;

        readonly List<GpuTrailEmitter> emitters = new List<GpuTrailEmitter>();
        readonly List<(Vector3, Vector3)> emitterPosLogs = new List<(Vector3, Vector3)>();
        NativeArray<Vector3> posArray;

        void Start()
        {
            RegisterGroup(this);
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (posArray != null) posArray.Dispose();
        }


        void CheckBuffers()
        {
            if (inputBuffer_Pos.count != bufferSize)
            {
                InitBuffers();
            }

            if (!posArray.IsCreated || posArray.Length != inputBuffer_Pos.count)
            {
                if (posArray.IsCreated) posArray.Dispose();
                posArray = new NativeArray<Vector3>(inputBuffer_Pos.count, Allocator.Persistent);
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

            while (emitterPosLogs.Count < emitters.Count)
            {
                emitterPosLogs.Add((default(Vector3), default(Vector3)));
            }


            var trailNum = gpuTrail.trailNum;

            for (var i = 0; i < emitters.Count; ++i)
            {
                var inputIdx = 0;
                var emitter = emitters[i];
                if (emitter != null && emitter.enabled)
                {
                    var pos = emitter.transform.position;

                    var (prev0, prev1) = emitterPosLogs[i];
                    var hasLog = prev0 != default && prev1 != default;
                    if (hasLog)
                    {
                        for (; inputIdx < inputCountMax; ++inputIdx)
                        {
                            var t = (float)(inputIdx + 1) / inputCountMax; //0:exclude 1:include
                            posArray[trailNum * inputIdx + i] = Spline.CatmullRom(t, prev1, prev0, pos);
                        }
                    }
                    else
                    {
                        posArray[i] = pos;
                        inputIdx++;
                    }

                    prev1 = prev0;
                    prev0 = pos;
                    emitterPosLogs[i] = (prev0, prev1);
                }

                // fill ignore point
                for (; inputIdx < inputCountMax; ++inputIdx)
                {
                    posArray[trailNum * inputIdx + i] = default;
                }
            }


            inputBuffer_Pos.SetData(posArray);

            return emitters.Any() ? inputCountMax : 0;
        }


        public void Register(GpuTrailEmitter emitter)
        {
            emitters.Add(emitter);
        }
    }
}