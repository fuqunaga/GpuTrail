#ifndef GPUTRAIL_VARIABLES_INCLUDED
#define GPUTRAIL_VARIABLES_INCLUDED

#include "GpuTrailVertex.cginc"

float4 _StartColor;
float4 _EndColor;
uint _VertexNumPerTrail;
StructuredBuffer<uint> _IndexBuffer;
StructuredBuffer<Vertex> _VertexBuffer;


Vertex GetVertex(uint indexBufferIdx, uint trailIdx)
{
	uint idx = _IndexBuffer[indexBufferIdx];
	idx += trailIdx * _VertexNumPerTrail;
	return _VertexBuffer[idx];
}

#endif // GPUTRAIL_VARIABLES_INCLUDED
