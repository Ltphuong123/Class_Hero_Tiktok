Shader "Comic/Sprite" {
	Properties {
		[PerRendererData] _MainTex ("MainTex", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[Header(Dissolve)] [Toggle(DISSOLVE_ON)] _UseDissolve ("Use Dissolve?", Float) = 0
		[KeywordEnum(LegacyBurn, Dissolve)] _DissoleType ("Dissolve Technology", Float) = 0
		[HideDisabled(DISSOLVE_ON)] [NoScaleOffset] _SliceGuide ("SliceGuide", 2D) = "bump" {}
		[HideDisabled(DISSOLVE_ON)] _SliceAmount ("SliceAmount", Range(0, 1)) = 0
		[HideDisabled(DISSOLVE_ON)] [NoScaleOffset] _BurnRamp ("BurnRamp", 2D) = "white" {}
		[HideDisabled(DISSOLVE_ON)] _BurnSize ("BurnSize", Float) = 0.15
		[HideDisabled(DISSOLVE_ON, _DISSOLETYPE_DISSOLVE)] _EdgeAroundHDR ("Edge Color HDR", Range(1, 5)) = 4
		[HideDisabled(DISSOLVE_ON, _DISSOLETYPE_DISSOLVE)] _EdgeAroundPower ("Edge Color Power", Range(1, 5)) = 1
		[HideDisabled(DISSOLVE_ON, _DISSOLETYPE_DISSOLVE)] _EdgeDistortion ("Edge Distortion", Range(0, 1)) = 0.1
		[HideDisabled(DISSOLVE_ON, _DISSOLETYPE_LEGACYBURN)] _BurnColor ("BurnColor", Vector) = (1,1,1,1)
		[HideDisabled(DISSOLVE_ON, _DISSOLETYPE_LEGACYBURN)] _BurnEmission ("BurnEmission", Float) = 1
		[Header(Gray)] [Toggle(GRAY_ON)] _EnableGray ("Use Gray?", Float) = 0
		[HideDisabled(GRAY_ON)] _Grayness ("Grayness", Range(0, 1)) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
		[Header(Stencil)] _Stencil ("Stencil ID", Float) = 0
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOpFail ("Stencil Fail Operation", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOpZFail ("Stencil Z-Fail Operation", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
}