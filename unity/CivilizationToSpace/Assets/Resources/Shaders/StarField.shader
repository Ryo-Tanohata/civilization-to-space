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
// **揺れは滑らかな波ではなく、短く強く光らせる。**
// sin をそのまま使うと、星が明るくなったり暗くなったりを
// ゆっくりくり返すだけで、瞬いているというより息をしているように見える。
// 速さの違う波を2つ掛けてから累乗でとがらせると、
// ふだんは控えめで、ときどき短く強く光る。これが「ピカピカ」に見える。
//
// **2つの波は割り切れない速さにする。** 同じ速さの倍数だと、
// 一定の間隔で規則正しく光り、電飾の点滅に見える。
// 割り切れない比にすると、光る間隔が毎回ずれて不規則になる。
//
// 速さ自体も粒ごとに変えている。そろえると、ばらばらに見えず
// 全体が脈打って見える。
//
// **実際の瞬きの仕組み（大気のゆらぎによる屈折）は表していない。**
// 光の点が不規則に光っている、ということだけを見せる。
Shader "CivilizationToSpace/StarField"
{
    Properties
    {
        _EmissionColor ("光の色と強さ", Color) = (1,1,1,1)
        _EmissionMap ("星の絵（アルファに揺れの位相）", 2D) = "white" {}
        _Color ("濃さ（アルファ）", Color) = (1,1,1,1)
        _TwinkleBase ("光っていないときの明るさ", Range(0,1)) = 0.5
        _TwinkleDepth ("光ったときに足す明るさ", Range(0,4)) = 2.0
        _TwinkleSharp ("光のとがり（大きいほど短く強く）", Range(1,8)) = 3.4
        _TwinkleSpeed ("揺れの速さ", Float) = 2.2
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
            half _TwinkleBase;
            half _TwinkleDepth;
            half _TwinkleSharp;
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
                half speed = _TwinkleSpeed * (0.45 + c.a * 1.7);

                // 割り切れない速さの波を2つ。光る間隔が毎回ずれる。
                half w1 = 0.5 + 0.5 * sin(_Time.y * speed + phase);
                half w2 = 0.5 + 0.5 * sin(_Time.y * speed * 0.37 + phase * 2.3);

                // 掛けてから累乗でとがらせる。ふだんは控えめ、ときどき短く強く。
                half spark = pow(w1 * w2, _TwinkleSharp);

                // 足す分を0にすると光らない。動きを減らす設定ではそうする。
                half twinkle = _TwinkleBase + _TwinkleDepth * spark;

                return fixed4(c.rgb * _EmissionColor.rgb * twinkle, _Color.a);
            }
            ENDCG
        }
    }

    FallBack Off
}
