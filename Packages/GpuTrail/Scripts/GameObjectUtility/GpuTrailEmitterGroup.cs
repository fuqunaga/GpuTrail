using System.Collections.Generic;
using System.Linq;
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

        public static void RegisterEmitter(string groupName, Transform transform)
        {
            if (groupDic.TryGetValue(groupName, out var trailGroup))
            {
                trailGroup.Register(transform);
            }
        }

        #endregion


        public string groupName;

        readonly List<Transform> units = new List<Transform>();

        Vector3[] posArray;

        void Start()
        {
            RegisterGroup(this);
        }

        protected override bool UpdateInputBuffer()
        {
            if (posArray == null)
            {
                posArray = new Vector3[gpuTrail.trailNum];
            }


            Assert.IsTrue(posArray.Length >= units.Count);

            for (var i = 0; i < units.Count; ++i)
            {
                var unit = units[i];
                if (unit != null)
                {
                    posArray[i] = unit.position;
                }
            }


            gpuTrail.inputBuffer_Pos.SetData(posArray);

            return units.Any();
        }


        public void Register(Transform trans)
        {
            units.Add(trans);
        }
    }
}