#ifndef OUTLINE_INCLUDE
#define OUTLINE_INCLUDE

TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);
float4 _CameraColorTexture_TexelSize;

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);


void DrawOutline_float(float2 UV, Texture2D MaterialData, SamplerState MaterialSampler, float OutlineThickness, float DepthDetectionThreshold, float NormalDetectionThreshold, bool ObjectIDDetection, out float OutlineValue, out float DepthValue, out float3 NormalValue, out float MaterialID)
{
#if defined(SHADERGRAPH_PREVIEW)
	OutlineValue = 0;
	DepthValue = 0;
	NormalValue = 0;
	MaterialID = 0;
#else
	OutlineValue = 0;
	
	float half_scale_floor = floor(OutlineThickness * 0.5);
	float half_scale_ceiling = ceil(OutlineThickness * 0.5);
	float2 texel_size = float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);

	float material_self = MaterialData.SampleLevel(MaterialSampler, UV, 0).r;
	MaterialID = material_self;
	float depth_self = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, UV, 0).r;
	DepthValue = depth_self;
	float3 normal_self = _NormalsData.SampleLevel(MaterialSampler, UV, 0).rgb;
	NormalValue = normal_self;
	
	float2 sample_positions[4];
	sample_positions[0] = UV - float2(texel_size.x, texel_size.y) * half_scale_floor;
	sample_positions[1] = UV + float2(texel_size.x, texel_size.y) * half_scale_ceiling;
	sample_positions[2] = UV + float2(texel_size.x * half_scale_ceiling, -texel_size.y * half_scale_floor);
	sample_positions[3] = UV + float2(-texel_size.x * half_scale_floor, texel_size.y * half_scale_ceiling);

	float material_samples[4];
	float depth_samples[4];
	float3 normal_samples[4];

	for (int i = 0; i < 4; i++) {
		material_samples[i] = MaterialData.SampleLevel(MaterialSampler, sample_positions[i], 0).r;
		depth_samples[i] = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, sample_positions[i], 0).r;
		normal_samples[i] = _NormalsData.SampleLevel(MaterialSampler, sample_positions[i], 0).rgb;
	}
	
	float mat_difference_0 = material_samples[1] - material_samples[0];
	float mat_difference_1 = material_samples[3] - material_samples[2];
	float matValue = sqrt(pow(mat_difference_0, 2) + pow(mat_difference_1, 2)) * 100;
	matValue = matValue > 0.001 ? 1 : 0;
	matValue = ObjectIDDetection == true ? matValue : 0;
	
	float depth_difference_0 = depth_samples[1] - depth_samples[0];
	float depth_difference_1 = depth_samples[3] - depth_samples[2];
	float depthValue = sqrt(pow(depth_difference_0, 2) + pow(depth_difference_1, 2)) * 100;
	depthValue = depthValue > DepthDetectionThreshold ? 1 : 0;
	
	float3 normal_difference_0 = normal_samples[1] - normal_samples[0];
	float3 normal_difference_1 = normal_samples[3] - normal_samples[2];
	float normalValue = sqrt(dot(normal_difference_0, normal_difference_0) + dot(normal_difference_1, normal_difference_1));
	normalValue = normalValue > NormalDetectionThreshold ? 1 : 0;
	
	float edge = max(matValue, depthValue);
	edge = max(edge, normalValue);
	OutlineValue = edge;
	
	
	#endif
}
#endif