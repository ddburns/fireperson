﻿Shader "Auras"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_Spec("Spec Color", Color) = (1, 1, 1, 1)
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Blend SrcAlpha One
		ZWrite Off
		Tags{ Queue = Transparent }
		ColorMask RGB
		// Vertex lights
		Pass
		{
			Tags{ "LightMode" = "Vertex" }
			Lighting On
			Material
			{
				Diffuse[_Color]
				Specular[_Spec]
			}

			SetTexture[_MainTex]
			{
				constantColor[_Color]
				Combine texture * primary DOUBLE, texture * constant
			}
		}
	}
	Fallback "VertexLit", 2
}