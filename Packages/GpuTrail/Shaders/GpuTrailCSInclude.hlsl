#ifndef GPU_TRAIL_CS_INCLUDED
#define GPU_TRAIL_CS_INCLUDED

struct Trail
{
	float startTime;
	uint totalInputNum;
};

struct Node
{
    float3 pos;
    float time;
	half4 color;
};

uint _TrailNum;
uint _NodeNumPerTrail;
float _MinNodeDistance;
float _Time;
float _Life;

uint calcTrailIdx(uint nodeIdx)
{
	return nodeIdx / _NodeNumPerTrail;
}
uint calcNodeIdx(uint trailIdx, uint nodeIdxInTrail)
{
	return (trailIdx * _NodeNumPerTrail) + (nodeIdxInTrail % _NodeNumPerTrail);
}

#endif // GPU_TRAIL_CS_INCLUDED
