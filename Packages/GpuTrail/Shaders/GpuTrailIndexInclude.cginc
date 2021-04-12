#ifndef GPUTRAIL_INDEX_INCLUDED
#define GPUTRAIL_INDEX_INCLUDED

// Put the following #pragma line in your .compute file and call [Shader/ComputeShader].EnableKeyword() on the C# side to make it work
// #pragma multi_compile __ GPUTRAIL_TRAIL_INDEX_ON

#ifdef GPUTRAIL_TRAIL_INDEX_ON

StructuredBuffer<uint> _TrailIndexBuffer;

inline uint GetTrailIdx(uint bufferIdx)
{
	return  _TrailIndexBuffer[bufferIdx];
}

#else

inline uint GetTrailIdx(uint bufferIdx)
{
	return bufferIdx;
}

#endif // GPUTRAIL_TRAIL_INDEX_ON

#endif // GPUTRAIL_INDEX_INCLUDED
