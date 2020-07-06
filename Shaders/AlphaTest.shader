Shader "Custom/Rickter/PPC/AlphaTest"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Int) = 2

		[Header(PBR)]
		_MainTex("Albedo", 2D) = "white" {}
		_MainTexColor("Albedo Tint", Color) = (1,1,1,0.5)
		_CutoffShift("Cutoff Shift", Range(0, 1)) = 0.5

//#region PPC Setup
		// R = Roughness
		// G = X tangent offset
		// B = Metalness
		// A = Y tangent offset
		// Sample G & A into a virtual object, then sqrt(1 - X^2 - Y^2) to remediate Z tangent offset

		_PPC("PPC", 2D) = "black" {}
		[MaterialToggle] _RoughnessVsSmoothness("Invert roughness", Int) = 0
		_MetalPower("Metal Power", Range( 0 , 5)) = 1
		_RoughnessPower("Roughness Power", Range( 0 , 5)) = 1
//#endregion

//#region AO Setup
		[MaterialToggle(PPC_AO_ENABLED)]_AmbientOcclusionEnabled("Ambient Occlusion", Int) = 0
		_AmbientOcclusion("AO Map", 2D) = "white" {}
		_AmbientOcclusionPower("Ambient Occlusion Power", Range( 0 , 5)) = 1
//#endregion


//#region Selfillum Setup
		[Header(Emission)]
		[MaterialToggle(_EMISSION)] _EmissionEnabled("SelfIllum Enabled", Int) = 0
		_EmissionMap("Selfillum Map", 2D) = "white" {}
		[HDR] _EmissionColor("Selfillum Tint", Color) = (1,1,1,1)
		_EmissionPack("Selfillum Pack", 2D) = "white" {}

		_EmissionPanSpeed("Selfillum Pan Speed", Vector) = (0,0,0,0)
		[MaterialToggle(PPC_EMISSION_OSCILLATOR_ENABLED)] _EmissionOscillate("Selfillum Oscillate", Int) = 0
		[MaterialToggle] _EmissionOscClampPeaks("Selfillum Osc Clamp Peaks", Int) = 1
		_EmissionOscPower("Selfillum Osc Amplitude", Range( -10 , 10)) = 1
		_EmissionOscSpeed("Selfillum Osc Freqency", Range( 0 , 100)) = 1
		[Toggle] _EmissionOscMode("Selfillum Osc Radial", Int) = 0
		[RemapSliders] _EmissionOscClamp("Selfillum Osc Clamp", Vector) = (0,1,0,0)
//#endregion Selfillum Setup

//#region Translucency Setup
		[Header(Translucency)]
		[MaterialToggle(PPC_TRANSLUCENCY_ENABLED)] _TranslucencyEnabled("Translucency Enabled", Int) = 0
		[Toggle] _InvertSS("Invert SS", Float) = 1
		_TranslucencyTint("Translucency Tint", Color) = (0,0,0,0)
		_TranslucencyMap("Translucency Map", 2D) = "black" {}
		_TranslucencyStrength("Strength", Range( 0 , 50)) = 1
		_TransNormalDistortion("Normal Distortion", Range( 0 , 1)) = 0.1
		_TransScattering("Scaterring Falloff", Range( 1 , 50)) = 2
		_TransDirect("Direct", Range( 0 , 1)) = 1
		_TransAmbient("Ambient", Range( 0 , 1)) = 0.2
		_TransShadow("Shadow", Range( 0 , 1)) = 0.9
//#endregion Translucency Setup

		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
// forward pass
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IsEmissive"="true" }
		Cull [_CullMode]
		ZWrite On

		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"

		#pragma target 5.0
		#pragma shader_feature _EMISSION
		#pragma shader_feature_local PPC_AO_ENABLED
		#pragma shader_feature_local PPC_EMISSION_OSCILLATOR_ENABLED
		#pragma shader_feature_local PPC_TRANSLUCENCY_ENABLED
		#pragma surface surf StandardCustom addshadow fullforwardshadows exclude_path:deferred alphatest:_CutoffShift

		struct Input
		{
			float2 uv_texcoord;
			INTERNAL_DATA
		};

		struct SurfaceOutputStandardCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half  Metallic;
			half  Smoothness;
			half  Occlusion;
			half  Alpha;
			Input SurfInput;
			half3 Translucency;
		};

		uniform int			_CullMode;

		uniform sampler2D	_MainTex;
		uniform float4		_MainTexColor;
		uniform float4		_MainTex_ST; // _ST = UV tiling

		uniform sampler2D	_PPC;

		uniform sampler2D	_AmbientOcclusion;
		uniform float		_AmbientOcclusionPower;

		uniform bool		_RoughnessVsSmoothness;
		uniform float		_RoughnessPower;
		uniform float		_MetalPower;

		uniform sampler2D	_EmissionMap;
		uniform sampler2D	_EmissionPack;
		uniform float2		_EmissionPanSpeed;
		uniform float4		_EmissionColor;
		uniform float		_EmissionOscillate;
		uniform float		_EmissionPower;
		uniform float		_EmissionOscPower;
		uniform float		_EmissionOscClampPeaks;
		uniform float		_EmissionOscSpeed;
		uniform int			_EmissionOscMode;
		uniform float2		_EmissionOscClamp;

		uniform sampler2D	_TranslucencyMap;
		uniform bool		_InvertSS;
		uniform float4		_TranslucencyTint;
		uniform half		_TranslucencyStrength;
		uniform half		_TransNormalDistortion;
		uniform half		_TransScattering;
		uniform half		_TransDirect;
		uniform half		_TransAmbient;
		uniform half		_TransShadow;


		inline half4 LightingStandardCustom(SurfaceOutputStandardCustom s, half3 viewDir, UnityGI gi )
		{
			#if PPC_TRANSLUCENCY_ENABLED
				#if !DIRECTIONAL
					float3 lightAtten = gi.light.color;
				#else
					float3 lightAtten = lerp( _LightColor0.rgb, gi.light.color, _TransShadow );
				#endif
		
				half3 lightDir = gi.light.dir + s.Normal * _TransNormalDistortion;
				half transVdotL = pow( saturate( dot( viewDir, -lightDir ) ), _TransScattering );
				half3 translucency = lightAtten * (transVdotL * _TransDirect + gi.indirect.diffuse * _TransAmbient) * s.Translucency;
		
				half4 c = half4( s.Albedo * translucency * _TranslucencyStrength, 0 );

				SurfaceOutputStandard r;

				r.Albedo 		= s.Albedo;
				r.Normal 		= s.Normal;
				r.Emission 		= s.Emission;
				r.Metallic 		= s.Metallic;
				r.Smoothness 	= s.Smoothness;
				r.Occlusion 	= s.Occlusion;
				r.Alpha 		= s.Alpha;

				return LightingStandard(r, viewDir, gi) + c;
			#else
				return LightingStandard((SurfaceOutputStandard) s, viewDir, gi);
			#endif
		}

		inline void LightingStandardCustom_GI(SurfaceOutputStandardCustom s, UnityGIInput data, inout UnityGI gi )
		{
			#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
				gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
			#else
				UNITY_GLOSSY_ENV_FROM_SURFACE( g, s, data );
				gi = UnityGlobalIllumination( data, s.Occlusion, s.Normal, g );
			#endif
		}

		// .x = .a
		// .y = .g
		// .z = .b
		// .w = .a
		inline fixed3 UnpackNormalPPC(fixed4 inPPC)
		{
			fixed3 normal;
			normal.xy = inPPC.wy * 2 - 1;
			normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));

			return normal;
		}


		void surf( Input i , inout SurfaceOutputStandardCustom o )
		{
			// all textures share coordinates, just reuse main UV for all maps
			float2 _MainTex_UV 	= i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;

			float4 albedo = tex2D(_MainTex, _MainTex_UV);
			//clip(albedo.a - _CutoffShift);

			o.Albedo = albedo.rgb * _MainTexColor.rgb;
			o.Alpha  = _MainTexColor.a * albedo.a;

			// normal map VO derived from PPC.g & PPC.a
			o.Normal = UnpackNormalPPC( tex2D(_PPC, _MainTex_UV) );


			o.Metallic = tex2D(_PPC, _MainTex_UV).b * _MetalPower;
			o.Smoothness = _RoughnessVsSmoothness ? tex2D(_PPC, _MainTex_UV).r * _RoughnessPower : 1 - tex2D(_PPC, _MainTex_UV).r * _RoughnessPower;


			#ifdef PPC_AO_ENABLED
				o.Occlusion = tex2D(_AmbientOcclusion, _MainTex_UV).r * _AmbientOcclusionPower;
			#endif


			#ifdef _EMISSION
				float2 panner = (_MainTex_UV + frac((_Time.y * _EmissionPanSpeed)));

				o.Emission =	tex2D(_EmissionMap, _MainTex_UV) *		// main texture
								tex2D(_EmissionPack, _MainTex_UV).r *	// masked by pack mask RED
								tex2D(_EmissionPack, panner).g * 		// then masked by pack mask GREEN
								_EmissionColor;							// and tinted

				#ifdef PPC_EMISSION_OSCILLATOR_ENABLED
					float oscSpeed = _EmissionOscSpeed;
					if (_EmissionOscMode > 0) {
						oscSpeed = 2 * UNITY_PI / _EmissionOscSpeed;
					}
					float sineWave = sin(oscSpeed * _Time.y);
					sineWave = _EmissionOscClampPeaks ? abs(sineWave) : sineWave;
					// ! waveform = sin(n * frequency) * amplitude
					// ! ideally, you want a radial frequency modifier for better looks: sin(2*pi/n*frequency)*amplitude

					float clampedWave = clamp(sineWave * _EmissionOscPower, _EmissionOscClamp.x, _EmissionOscClamp.y);

					float4 oscillatedMaskedPannedEmission =
					(
						(
							tex2D( _EmissionMap, _MainTex_UV ) *
							tex2D( _EmissionPack, _MainTex_UV ).r *
							tex2D( _EmissionPack, panner ).g
						) *
						_EmissionColor *
						(
							_EmissionOscillate ? clampedWave : _EmissionColor.a
						)
					);

					o.Emission = oscillatedMaskedPannedEmission.rgb;
				#endif
			#endif


			#ifdef PPC_TRANSLUCENCY_ENABLED
				float4 ssMap = tex2D(_TranslucencyMap, _MainTex_UV);
				ssMap =  _InvertSS ? 1 - ssMap : ssMap;
				o.Translucency = ssMap.rgb * _TranslucencyTint;
			#endif


			// uncomment lines below to force "Debug" output of a specific map
			// o.SurfInput = i;
			// o.Albedo = float3(0,0,0);
			// o.Normal = float3(0,0,1);
			// o.Emission = o.Occlusion.xxx + 1E-5;
		}

		ENDCG

		// Meta Pass exposes emission data to GI baker so we can influence both RGI and BGI
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Off

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			#pragma shader_feature _EMISSION

			#include "UnityStandardMeta.cginc"
			ENDCG
		}
	}
	Fallback "Standard"
	CustomEditor "PPCShaderGUI_AlphaTest"
}