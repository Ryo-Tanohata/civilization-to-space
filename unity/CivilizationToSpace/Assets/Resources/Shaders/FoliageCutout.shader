// 葉の板。絵の抜き色で切り抜く。
//
// **なぜ専用のシェーダーが要るのか。**
// 写真計測の木は、葉を1枚ずつ作らずに、板へ葉の絵を貼って
// 透明なところを捨てる作りになっている。切り抜きをしないと、
// 葉のまわりの四角い板がそのまま出て、木が板の塊に見える。
//
// **なぜStandardを使わないのか。**
// このプロジェクトの他のシェーダーと同じ理由である。WebGLビルドでは
// Standard のキーワードで分かれた実体が残らず、色も発光も画面に出なかった。
// ここでも分岐を作らず、実体をひとつだけにする。
//
// 切り抜きは alphatest で行う。半透明で重ねると、葉どうしの前後が
// 描く順で入れ替わってちらつく。切り抜きなら深度が正しく書かれる。
//
// **裏表を切り捨てない。** 葉の板は片面しかないので、
// 裏から見たときに消えてしまう。
Shader "CivilizationToSpace/FoliageCutout"
{
    Properties
    {
        _Color ("色", Color) = (1,1,1,1)
        _MainTex ("葉の絵（アルファで切り抜く）", 2D) = "white" {}
        _Cutoff ("切り抜く境目", Range(0,1)) = 0.45
        _Glossiness ("滑らかさ", Range(0,1)) = 0
        _Metallic ("金属らしさ", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }

    FallBack Off
}
