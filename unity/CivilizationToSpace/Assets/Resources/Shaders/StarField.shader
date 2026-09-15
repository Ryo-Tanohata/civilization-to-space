// 夜空の星。加算で光を足すだけだが、**星ごとに明るさが揺れる。**
//
// なぜ専用のシェーダーが要るのか。
// 加算の層（AdditiveGlow）は材質ごとに1つの明るさしか持てないため、
// これで星を描くと空ぜんたいが同じ明るさで点滅する。空が呼吸しているように見え、
// 星が瞬いているようには見えない。星は1粒ずつ別々に揺れなければならない。
//
// **揺れの位相は絵のアルファに焼き込んである。** 位置から乱数で作ると、
// 同じ星の中心と十字のにじみが別々の位相になり、粒が分解して見える。
// 絵を作るときに1粒へ同じ値を書いておけば、粒ごとにきれいにそろう。
//
// 揺れる速さも位相からずらしている。同じ速さだと、ばらばらに見えず
// 全体が脈打って見える。
//
// **実際の瞬きの仕組み（大気のゆらぎによる屈折）は表していない。**
// 光の点が揺れている、ということだけを見せる。
Shader "CivilizationToSpace/StarField"
{
    Properties
    {
        _EmissionColor ("光の色と強さ", Color) = (1,1,1,1)
        _EmissionMap ("星の絵（アルファに揺れの位相）", 2D) = "white" {}
        _Color ("濃さ（アルファ）", Color) = (1,1,1,1)
        _TwinkleDepth ("揺れの深さ", Range(0,1)) = 0.55
        _TwinkleSpeed ("揺れの速さ", Float) = 2.6
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
            half _TwinkleDepth;
            half _TwinkleSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _EmissionMap);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_EmissionMap, i.uv);

                // アルファに焼いた位相。0〜1を一周ぶんに広げる。
                half phase = c.a * 6.2831853;

                // 速さも位相でずらす。そろえると全体が脈打って見える。
                half speed = _TwinkleSpeed * (0.55 + c.a);
                half wave = 0.5 + 0.5 * sin(_Time.y * speed + phase);

                // 深さ0で揺れない。動きを減らす設定のときは0を渡す。
                half twinkle = 1.0 - _TwinkleDepth + _TwinkleDepth * wave;

                return fixed4(c.rgb * _EmissionColor.rgb * twinkle, _Color.a);
            }
            ENDCG
        }
    }

    FallBack Off
}
