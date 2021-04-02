#ifndef GPUTRAIL_CULLING_INCLUDED
#define GPUTRAIL_CULLING_INCLUDED


#ifdef GPUTRAIL_TRAIL_INDEX_ON

StructuredBuffer<uint> _TrailIndexBuffer;

inline uint GetTrailIdxWithCulling(uint bufferIdx)
{
	return _TrailIndexBuffer[bufferIdx];
}

#else

inline uint GetTrailIdxWithCulling(uint bufferIdx)
{
	return bufferIdx;
}

#endif


#endif // GPUTRAIL_CULLING_INCLUDED
