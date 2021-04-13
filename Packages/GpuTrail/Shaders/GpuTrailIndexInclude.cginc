#ifndef GPUTRAIL_INDEX_INCLUDED
#define GPUTRAIL_INDEX_INCLUDED

// Put the following #pragma line in your .compute file and call [Shader/ComputeShader].EnableKeyword() on the C# side to make it work
// #pragma multi_compile __ GPUTRAIL_TRAIL_INDEX_ON

#ifdef GPUTRAIL_TRAIL_INDEX_ON


StructuredBuffer<uint> _TrailIndexBuffer;
ByteAddressBuffer _TrailNumBuffer; // It is a buffer because it gets its value from GraphicsBuffer.CopyCount.

inline uint GetTrailIdx(uint bufferIdx)
{
	return  _TrailIndexBuffer[bufferIdx];
}

inline uint GetTrailNum()
{
	return _TrailNumBuffer.Load(0);
}

#else


#include "GpuTrailCSInclude.cginc"

inline uint GetTrailIdx(uint bufferIdx)
{
	return bufferIdx;
}

inline uint GetTrailNum()
{
	return _TrailNum;
}

#endif // GPUTRAIL_TRAIL_INDEX_ON


#endif // GPUTRAIL_INDEX_INCLUDED
