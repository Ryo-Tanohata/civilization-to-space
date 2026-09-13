// 加算で光を足すだけの層。溶岩のにじみと、ぶつかった跡の光に使う。
//
// 明るさの計算はしない。光そのものなので、どちらから照らされているかは関係ない。
// 加算は「重なったところが明るくなる」だけなので、不透明度の扱いに左右されない。
Shader "CivilizationToSpace/AdditiveGlow"
{
    Properties
    {
        _EmissionColor ("光の色と強さ", Color) = (1,1,1,1)
        _EmissionMap ("光る形", 2D) = "white" {}
        _Color ("濃さ（アルファ）", Color) = (1,1,1,1)
        _CutCenter ("削る中心（ワールド座標）", Vector) = (0,0,0,0)
        _CutRadius ("削る半径", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100

        Blend SrcAlpha One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;
            fixed4 _EmissionColor;
            fixed4 _Color;
            float4 _CutCenter;
            half _CutRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _EmissionMap);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 地球が欠けた部分では、にじみも出さない。
                if (_CutRadius > 0)
                {
                    clip(distance(i.worldPos, _CutCenter.xyz) - _CutRadius);
                }

                fixed3 lit = tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                return fixed4(lit, _Color.a);
            }
            ENDCG
        }
    }

    FallBack Off
}
