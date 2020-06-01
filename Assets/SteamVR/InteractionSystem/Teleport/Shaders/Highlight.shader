//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Used for the teleport markers
//
//=============================================================================
// UNITY_SHADER_NO_UPGRADE
Shader "Valve/VR/Highlight"
{
	Properties
	{
		_TintColor( "Tint Color", Color ) = ( 1, 1, 1, 1 )
		_SeeThru( "SeeThru", Range( 0.0, 1.0 ) ) = 0.25
		_Darken( "Darken", Range( 0.0, 1.0 ) ) = 0.0
		_MainTex( "MainTex", 2D ) = "white" {}
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
	HLSLINCLUDE
		
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Required to compile gles 2.0 with standard SRP library
        // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #pragma target 2.0

        // -------------------------------------
        // Material Keywords
        // unused shader_feature variants are stripped from build automatically
        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _ALPHATEST_ON
        #pragma shader_feature _ALPHAPREMULTIPLY_ON
        #pragma shader_feature _EMISSION
        #pragma shader_feature _METALLICSPECGLOSSMAP
        #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #pragma shader_feature _OCCLUSIONMAP

        #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
        #pragma shader_feature _GLOSSYREFLECTIONS_OFF
        #pragma shader_feature _SPECULAR_SETUP
        #pragma shader_feature _RECEIVE_SHADOWS_OFF

        // -------------------------------------
        // Universal Render Pipeline keywords
        // When doing custom shaders you most often want to copy and past these #pragmas
        // These multi_compile variants are stripped from the build depending on:
        // 1) Settings in the LWRP Asset assigned in the GraphicsSettings at build time
        // e.g If you disable AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
        // will be stripped from build
        // 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
        // but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

        // -------------------------------------
        // Unity defined keywords
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile_fog

        //--------------------------------------
        // GPU Instancing
        #pragma multi_compile_instancing

		// Including the following two function is enought for shading with Universal Pipeline. Everything is included in them.
        // Core.hlsl will include SRP shader library, all constant buffers not related to materials (perobject, percamera, perframe).
        // It also includes matrix/space conversion functions and fog.
        // Lighting.hlsl will include the light functions/data to abstract light constants. You should use GetMainLight and GetLight functions
        // that initialize Light struct. Lighting.hlsl also include GI, Light BDRF functions. It also includes Shadows.

        // Required by all Universal Render Pipeline shaders.
        // It will include Unity built-in shader variables (except the lighting variables)
        // (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
        // It will also include many utilitary functions. 
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // Include this if you are doing a lit shader. This includes lighting shader variables,
        // lighting and shadow functions
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // Material shader variables are not defined in SRP or LWRP shader library.
        // This means _BaseColor, _BaseMap, _BaseMap_ST, and all variables in the Properties section of a shader
        // must be defined by the shader itself. If you define all those properties in CBUFFER named
        // UnityPerMaterial, SRP can cache the material properties between frames and reduce significantly the cost
        // of each drawcall.
        // In this case, for sinmplicity LitInput.hlsl is included. This contains the CBUFFER for the material
        // properties defined above. As one can see this is not part of the ShaderLibrary, it specific to the
        // LWRP Lit shader.
        #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

		// Pragmas --------------------------------------------------------------------------------------------------------------------------------------------------
		/*#pragma target 5.0
		#pragma only_renderers d3d11 vulkan glcore
		#pragma exclude_renderers gles*/

		// Includes -------------------------------------------------------------------------------------------------------------------------------------------------
		//#include "UnityCG.cginc"

		// Structs --------------------------------------------------------------------------------------------------------------------------------------------------
		struct VertexInput
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			float4 color : COLOR;
		};
		
		struct VertexOutput
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			float4 color : COLOR;
		};

		// Globals --------------------------------------------------------------------------------------------------------------------------------------------------
		sampler2D _MainTex;
		float4 _MainTex_ST;
		float4 _TintColor;
		float _SeeThru;
		float _Darken;
				
		// MainVs ---------------------------------------------------------------------------------------------------------------------------------------------------
		VertexOutput MainVS( VertexInput i )
		{
			VertexOutput o;
//#if UNITY_VERSION >= 540
//			o.vertex = UnityObjectToClipPos(i.vertex);
//#else
			o.vertex = mul(UNITY_MATRIX_MVP, i.vertex);
//#endif
			o.uv = TRANSFORM_TEX( i.uv, _MainTex );
			o.color = i.color;
			
			return o;
		}
		
		// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
		float4 MainPS( VertexOutput i ) : SV_Target
		{
			float4 vTexel = tex2D( _MainTex, i.uv ).rgba;
			float4 vColor = vTexel.rgba * _TintColor.rgba * i.color.rgba;
			vColor.rgba = saturate( 2.0 * vColor.rgba );
			float flAlpha = vColor.a;

			vColor.rgb *= vColor.a;
			vColor.a = lerp( 0.0, _Darken, flAlpha );

			return vColor.rgba;
		}

		// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
		float4 SeeThruPS( VertexOutput i ) : SV_Target
		{
			float4 vTexel = tex2D( _MainTex, i.uv ).rgba;
			float4 vColor = vTexel.rgba * _TintColor.rgba * i.color.rgba * _SeeThru;
			vColor.rgba = saturate( 2.0 * vColor.rgba );
			float flAlpha = vColor.a;

			vColor.rgb *= vColor.a;
			vColor.a = lerp( 0.0, _Darken, flAlpha * _SeeThru );

			return vColor.rgba;
		}

	ENDHLSL

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType"="Transparent" "Queue" = "Transparent" }

		LOD 100

		// Behind Geometry ---------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Name "BehindPass"
			Tags { "LightMode" = "HighlightBehindPass" }
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend One OneMinusSrcAlpha
			Cull Off
			ZWrite Off
			ZTest Greater

			HLSLPROGRAM
				#pragma vertex MainVS
				#pragma fragment SeeThruPS
			ENDHLSL
		}

		Pass
		{
			Name "FrontPass"
			Tags { "LightMode" = "HighlightFrontPass" }
			// Render State ---------------------------------------------------------------------------------------------------------------------------------------------
			Blend One OneMinusSrcAlpha
			Cull Off
			ZWrite Off
			ZTest LEqual

			HLSLPROGRAM
				#pragma vertex MainVS
				#pragma fragment MainPS
			ENDHLSL
		}
	}
}
