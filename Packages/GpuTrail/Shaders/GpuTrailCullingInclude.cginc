#ifndef GPUTRAIL_CULLING_INCLUDED
#define GPUTRAIL_CULLING_INCLUDED

// Put the following #pragma line in your .compute file and call [Shader/ComputeShader].EnableKeyword() on the C# side to make it work
// #pragma multi_compile_local __ GPUTRAIL_TRAIL_INDEX_ON

#ifdef GPUTRAIL_TRAIL_INDEX_ON

StructuredBuffer<uint> _TrailIndexBuffer;

inline uint GetTrailIdxWithCulling(uint bufferIdx)
{
	return  _TrailIndexBuffer[bufferIdx];
}

#else

inline uint GetTrailIdxWithCulling(uint bufferIdx)
{
	return bufferIdx;
}

#endif // GPUTRAIL_CULLING_ENABLE

#endif // GPUTRAIL_CULLING_INCLUDED
