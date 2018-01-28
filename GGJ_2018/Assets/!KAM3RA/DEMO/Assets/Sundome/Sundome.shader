// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Sundome" 
{
	Properties 
	{
		_SkyColor ("Sky Color", Color) 			= (0.0, 0.5, 1.0, 1.0)		
		_Scatter ("Scatter", Color) 			= (1.0, 1.0, 1.0, 1.0)		
		_Light ("Light Position", Vector) 		= (0.0, 1.0, 0.0)
		_LightNormal ("Light Normal", Vector) 	= (0.0, 1.0, 0.0)
		_Origin ("Origin", Vector) 				= (0, 1, 0)
		_Size ("Size", float) 					= 1.0
		_Power ("Power", float) 				= 2.0
		_ArcLength ("ArcLength", float) 		= 0.0		
	}	
	Category 
	{
		Tags { "Queue"="Background" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend Off ColorMask RGB Lighting Off ZWrite Off Fog { Mode Off } Cull Off // Off Back
		BindChannels 
		{
			Bind "Vertex", vertex
		}	
		SubShader 
		{
			Pass 
			{			
				CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members pos)
#pragma exclude_renderers d3d11 xbox360
				#pragma vertex vert
				#pragma fragment frag				
				#include "UnityCG.cginc"	
				fixed4 	_SkyColor;								
				fixed4 	_Scatter;							
				float4 	_Light;	
				float3 	_LightNormal;							
				float3 	_Origin;				
				float 	_Size;				
				float 	_Power;					
				float 	_ArcLength;				
				struct appdata_t 
				{
					float4 vertex : POSITION;
				};
				struct v2f 
				{
					float4 vertex : POSITION;
					float3 pos;					
				};	
				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex 	= UnityObjectToClipPos(v.vertex);		
					o.pos		= v.vertex.xyz; 
					return o;
				}					
				fixed4 frag (v2f i) : COLOR
				{
					float4 color 		= float4(1);
					
					float3 normal 		= normalize(i.pos);					
					float3 light		= normalize(_LightNormal - i.pos);					
					float sun 			= saturate(dot(normal, light));	
					float arclen 		= acos(dot(normal, _Origin));
					arclen 				= (arclen + _ArcLength) * 0.5;						
					color.rgb			= lerp(_SkyColor.rgb, _Scatter.rgb, arclen);	
					color.rgb 			= lerp(color.rgb, _Scatter.rgb * _Power, pow(sun, _Size));					
					return color;
				}
				ENDCG 
			}
		} 		
	}
}

