Shader "Comic/3DSurface" {
	Properties {
		_Shininess ("Shininess", Range(0.03, 1)) = 0.13
		[NoScaleOffset] _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		[Header(Emission)] [Space(5)] [Toggle(EMOSSION_ON)] _EnableEmossion ("Emission", Float) = 0
		[HideDisabled(EMOSSION_ON)] [NoScaleOffset] _EmissionMap ("Emission Texture", 2D) = "bump" {}
		[HideDisabled(EMOSSION_ON)] _EmissionPower ("Emission Power", Range(0, 5)) = 2
		[Header(Dissolve)] [Space(5)] [Toggle(DISSOLVE_ON)] _EnableDissolve ("Use Dissolve?", Float) = 0
		[HideDisabled(DISSOLVE_ON)] _DissolveTex ("Dissolve Texture(RGB)", 2D) = "white" {}
		[HideDisabled(DISSOLVE_ON)] _SliceAmount ("Dissolve Amount", Range(0, 1)) = 0
		[HideDisabled(DISSOLVE_ON)] [HDR] _DissolveFirstColor ("Dissolve First Color", Vector) = (0.9,0.9,0.9,1)
		[HideDisabled(DISSOLVE_ON)] [HDR] _DissolveSecondColor ("Dissolve Second Color", Vector) = (1,1.13,1.13,1)
		[HideDisabled(DISSOLVE_ON)] _LineWidth ("Dissolve Edge Size", Range(0, 0.2)) = 0.1
		[Space(5)] [Toggle(_DISSOLVE_WORLDPOS_ON)] _EnableDissolveWorldPos ("Use Dissolve WorldPos?", Float) = 0
		[HideDisabled(_DISSOLVE_WORLDPOS_ON)] _WorldDissolveAmount ("WorldDissolve Amount", Range(-3, 3)) = 0
		[HideDisabled(_DISSOLVE_WORLDPOS_ON)] _WorldDissolveDirection ("WorldDissolve Direction", Vector) = (0,1,0,0)
		[Header(Gray)] [Space(5)] [Toggle(GRAY_ON)] _EnableGray ("Use Gray?", Float) = 0
		[HideDisabled(GRAY_ON)] _Grayness ("Grayness", Range(0, 1)) = 0
		[HideDisabled(GRAY_ON)] _GrayDotColor ("Gray dot Color", Vector) = (0.227,0,0,1)
		[Header(SelfLight)] [Space(5)] [Toggle(SELF_LIGHT_ON)] _EnableSelfLight ("Enable self light?", Float) = 0
		[HideDisabled(SELF_LIGHT_ON)] [HDR] _SelfLightColor ("Color", Vector) = (1.74,2.8,3.38,1)
		[HideDisabled(SELF_LIGHT_ON)] _SelfLightness ("Lightness", Range(0, 1)) = 0
		[Header(Rim)] [Space(5)] _RimColor ("Rim Color", Vector) = (1,1,1,1)
		_RimLightRotation ("Rim Light Rotation", Range(0, 360)) = 0
		_RimLightSoft ("Rim Light Soft", Range(0, 0.7)) = 0
		_RimLightThreshold ("Rim Light Threshold", Range(0.8, 1)) = 1
		_RimPower ("Rim Power", Float) = 1
		_RimBias ("Rim Bias", Float) = 0
		_RimScale ("Rim Scale", Float) = 0
		_CenterOffset ("Center Offset", Float) = 0
		[Header(Stencil)] _Stencil ("Stencil ID", Float) = 2
		_StencilReadMask ("Stencil Read Mask", Float) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 2
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

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
	Fallback "Mobile/Bumped Diffuse"
}