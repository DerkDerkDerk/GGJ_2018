// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//////////////////////////////////////////////////////////////
//  3PCAM Third-Person Camera System
// 	Copyright © 2013 Regress Software
// 	This is a simple two-pass shader to add a drop shadow
//////////////////////////////////////////////////////////////

Shader "GUI/3D Text Shader" 
{
    Properties 
    {
        _MainTex ("Font Texture", 2D) 					= "white" {}
        _Color ("Text Color", Color) 					= (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) 			= (0,0,0,1)
        _ShadowPositionX ("Shadow Position X", float)	= 0.75
        _ShadowPositionY ("Shadow Position Y", float)	= 0.75
   }
    SubShader 
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off Cull Off ZWrite Off Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass 
        {    
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            struct appdata_t 
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f 
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform float4 _ShadowColor;
            uniform float _ShadowPositionX;
            uniform float _ShadowPositionY;
            v2f vert (appdata_t v)
            {
                v2f o;
                float4 vertex 	= v.vertex;
                vertex.x 		+= _ShadowPositionX * 0.01;
                vertex.y 		-= _ShadowPositionY * 0.01;
                o.vertex 		= UnityObjectToClipPos(vertex);
                o.texcoord 		= TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            float4 frag (v2f i) : COLOR
            {
                float4 col 		= _ShadowColor;
                col.a 			*= tex2D(_MainTex, i.texcoord).a;
                return col;
            }
            ENDCG 
        }
        Pass 
        {    
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            struct appdata_t 
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f 
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform float4 _ShadowColor;
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
                return o;
            }
            float4 frag (v2f i) : COLOR
            {
                float4 col = _Color;
                col.a *= tex2D(_MainTex, i.texcoord).a;
                return col;
            }
            ENDCG 
        }
    }     
    Fallback "Custom/3D Text Shader Simple"
}