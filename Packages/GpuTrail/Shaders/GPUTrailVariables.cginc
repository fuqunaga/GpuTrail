#ifndef GPUTRAIL_VARIABLES_INCLUDED
#define GPUTRAIL_VARIABLES_INCLUDED

#include "GpuTrailVertex.cginc"

float4 _StartColor;
float4 _EndColor;
uint _VertexNumPerTrail;
StructuredBuffer<uint> _IndexBuffer;
StructuredBuffer<Vertex> _VertexBuffer;

#ifdef GPUTRAIL_TRAIL_INDEX_ON
StructuredBuffer<uint> _TrailIndexBuffer;
#endif

Vertex GetVertex(uint indexBufferIdx, uint trailIdx){
#ifdef GPUTRAIL_TRAIL_INDEX_ON
	trailIdx = _TrailIndexBuffer[trailIdx];
#endif
	uint idx = _IndexBuffer[indexBufferIdx];
	idx += trailIdx * _VertexNumPerTrail;
	return _VertexBuffer[idx];
}

#endif // GPUTRAIL_VARIABLES_INCLUDED
