//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Used to show the outline of the object
//
//=============================================================================
// UNITY_SHADER_NO_UPGRADE
Shader "Valve/VR/Silhouette"
{
	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
	Properties
	{
		g_vOutlineColor( "Outline Color", Color ) = ( .5, .5, .5, 1 )
		g_flOutlineWidth( "Outline width", Range ( .001, 0.03 ) ) = .005
		g_flCornerAdjust( "Corner Adjustment", Range( 0, 2 ) ) = .5
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

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		//#include "UnityCG.cginc"

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		float4 g_vOutlineColor;
		float g_flOutlineWidth;
		float g_flCornerAdjust;

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		struct VS_INPUT
		{
			float4 vPositionOs : POSITION;
			float3 vNormalOs : NORMAL;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		struct PS_INPUT
		{
			float4 vPositionOs : TEXCOORD0;
			float3 vNormalOs : TEXCOORD1;
			float4 vPositionPs : SV_POSITION;
		};

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		PS_INPUT MainVs( VS_INPUT i )
		{
			PS_INPUT o;
			o.vPositionOs.xyzw = i.vPositionOs.xyzw;
			o.vNormalOs.xyz = i.vNormalOs.xyz;
//#if UNITY_VERSION >= 540
//			o.vPositionPs = UnityObjectToClipPos( i.vPositionOs.xyzw );
//#else
			o.vPositionPs = mul( UNITY_MATRIX_MVP, i.vPositionOs.xyzw );
//#endif
			return o;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		PS_INPUT Extrude( PS_INPUT vertex )
		{
			PS_INPUT extruded = vertex;

			// Offset along normal in projection space
			float3 vNormalVs = mul( ( float3x3 )UNITY_MATRIX_IT_MV, vertex.vNormalOs.xyz );
			//float2 vOffsetPs = vNormalVs.xy;//TransformViewToProjection( vNormalVs.xy );
			float2 vOffsetPs = mul( (float3x3)UNITY_MATRIX_P, vNormalVs).xy;
			vOffsetPs.xy = normalize( vOffsetPs.xy );

			// Calculate position
//#if UNITY_VERSION >= 540
//			extruded.vPositionPs = UnityObjectToClipPos( vertex.vPositionOs.xyzw );
//#else
			extruded.vPositionPs = mul( UNITY_MATRIX_MVP, vertex.vPositionOs.xyzw );
//#endif
			extruded.vPositionPs.xy += vOffsetPs.xy * extruded.vPositionPs.w * g_flOutlineWidth;

			return extruded;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		[maxvertexcount(18)]
		void ExtrudeGs( triangle PS_INPUT inputTriangle[3], inout TriangleStream<PS_INPUT> outputStream )
		{
		    float3 a = normalize(inputTriangle[0].vPositionOs.xyz - inputTriangle[1].vPositionOs.xyz);
		    float3 b = normalize(inputTriangle[1].vPositionOs.xyz - inputTriangle[2].vPositionOs.xyz);
		    float3 c = normalize(inputTriangle[2].vPositionOs.xyz - inputTriangle[0].vPositionOs.xyz);

		    inputTriangle[0].vNormalOs = inputTriangle[0].vNormalOs + normalize( a - c) * g_flCornerAdjust;
		    inputTriangle[1].vNormalOs = inputTriangle[1].vNormalOs + normalize(-a + b) * g_flCornerAdjust;
		    inputTriangle[2].vNormalOs = inputTriangle[2].vNormalOs + normalize(-b + c) * g_flCornerAdjust;

		    PS_INPUT extrudedTriangle0 = Extrude( inputTriangle[0] );
		    PS_INPUT extrudedTriangle1 = Extrude( inputTriangle[1] );
		    PS_INPUT extrudedTriangle2 = Extrude( inputTriangle[2] );

		    outputStream.Append( inputTriangle[0] );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( inputTriangle[1] );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( extrudedTriangle1 );
		    outputStream.Append( inputTriangle[1] );

		    outputStream.Append( inputTriangle[1] );
		    outputStream.Append( extrudedTriangle1 );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle[1] );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( inputTriangle[2] );

		    outputStream.Append( inputTriangle[2] );
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append(inputTriangle[0]);
		    outputStream.Append( extrudedTriangle2 );
		    outputStream.Append( extrudedTriangle0 );
		    outputStream.Append( inputTriangle[0] );
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		float4 MainPs( PS_INPUT i ) : SV_Target
		{
			return g_vOutlineColor;
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		float4 NullPs( PS_INPUT i ) : SV_Target
		{
			return float4( 1.0, 0.0, 1.0, 1.0 );
		}

	ENDHLSL

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True" "RenderType"="Opaque" "Queue" = "Geometry-1" }

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Render the object with stencil=1 to mask out the part that isn't the silhouette
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Name "StencilPass"
			Tags { "LightMode" = "SilhouetteStencilPass" }
			ColorMask 0
			Cull Off
			ZWrite Off
			Stencil
			{
				Ref 1
				Comp always
				Pass replace
			}

			HLSLPROGRAM
				#pragma vertex MainVs
				#pragma fragment NullPs
			ENDHLSL
		}

		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Render the outline by extruding along vertex normals and using the stencil mask previously rendered. Only render depth, so that the final pass executes
		// once per fragment (otherwise alpha blending will look bad).
		//-------------------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Name "SilhouetteExtrudePass"
			Tags { "LightMode" = "SilhouetteExtrudePass" }
			Cull Off
			ZWrite On
			Stencil
			{
				Ref 1
				Comp notequal
				Pass keep
				Fail keep
			}

			HLSLPROGRAM
				#pragma require geometry

				#pragma vertex MainVs
				#pragma geometry ExtrudeGs
				#pragma fragment MainPs
			ENDHLSL
		}
	}
}