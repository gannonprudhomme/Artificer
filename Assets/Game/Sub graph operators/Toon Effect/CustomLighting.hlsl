// Retrieved from: https://github.com/UnityTechnologies/open-project-1/tree/devlogs/1-toon-shading

#ifndef CUSTOM_LIGHTING_INCLUDED // idk what this does
#define CUSTOM_LIGHTING_INCLUDED

void MainLight_float(
    float3 WorldPos,
    float3 WorldNormal,
    float3 WorldView,
    float Smoothness,

    out float3 Direction,
    out float3 Color,
    out float DistanceAtten,
    out float ShadowAtten,

    out float3 Specular
) {
#ifdef SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
    DistanceAtten = 1;
    ShadowAtten = 1;
    Specular = float3(1, 1, 1);
#else
	float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    Smoothness = exp2(10 * Smoothness + 1);

    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;

	#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
		ShadowAtten = 1.0h;
	#else
        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        float shadowStrength = GetMainLightShadowStrength();
        ShadowAtten = SampleShadowmap(
            shadowCoord,
            TEXTURE2D_ARGS(
                _MainLightShadowmapTexture,
                sampler_MainLightShadowmapTexture
            ),
            shadowSamplingData,
            shadowStrength,
            false
        );

        float3 radiance = mainLight.color * (mainLight.distanceAttenuation * ShadowAtten);
        Specular = LightingSpecular(
            radiance,
            mainLight.direction,
            WorldNormal,
            WorldView,
            float4(1, 1, 1, 0),
            Smoothness
        );
    #endif
#endif
}

void AdditionalLights_float(
    float3 SpecColor,
    float Smoothness,
    float3 WorldPosition,
    float3 WorldNormal,
    float3 WorldView,

    out float3 Diffuse,
    out float3 Specular
) {
    float3 diffuseColor = 0;
    float3 specularColor = 0;
#ifndef SHADERGRAPH_PREVIEW
    Smoothness = exp2(10 * Smoothness + 1);
    uint pixelLightCount = GetAdditionalLightsCount();
	// LIGHT_LOOP_BEGIN(pixelLightCount) {
    for(uint lightIndex = 0; lightIndex < pixelLightCount; lightIndex++) {
		Light light = GetAdditionalLight(lightIndex, WorldPosition, 1);
        // Blinn-Phong
        float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        diffuseColor += LightingLambert(
            attenuatedLightColor,
            light.direction,
            WorldNormal
        );
        specularColor += LightingSpecular(
            attenuatedLightColor,
            light.direction,
            WorldNormal,
            WorldView,
            float4(SpecColor, 0),
            Smoothness
        );
    } //LIGHT_LOOP_END
#endif

	Diffuse = diffuseColor;
	Specular = specularColor;
}

#endif

