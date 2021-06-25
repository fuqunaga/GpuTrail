#ifndef GPUTRAIL_VARIABLES_INCLUDED
#define GPUTRAIL_VARIABLES_INCLUDED

#include "GpuTrailVertex.cginc"

float _StartWidth;
float _EndWidth;

uint _VertexNumPerTrail;
StructuredBuffer<Vertex> _VertexBuffer;


Vertex GetVertex(uint vertexIdx, uint trailIdx)
{
	uint idx = vertexIdx +  (trailIdx * _VertexNumPerTrail);
	return _VertexBuffer[idx];
}

#endif // GPUTRAIL_VARIABLES_INCLUDED
