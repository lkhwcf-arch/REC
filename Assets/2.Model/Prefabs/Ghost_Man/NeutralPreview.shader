Shader "MaskedHumanoid/NeutralPreview" {
    Properties { _Color ("Color",Color)=(.65,.68,.71,1) }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION;float3 n:TEXCOORD0;float3 view:TEXCOORD1; };
            fixed4 _Color;
            v2f vert(appdata v) {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.n=UnityObjectToWorldNormal(v.normal);o.view=_WorldSpaceCameraPos-mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(v2f i):SV_Target {float3 n=normalize(i.n);float light=.35+.6*saturate(dot(n,normalize(float3(-.4,1,.6))));float rim=pow(1-saturate(dot(n,normalize(i.view))),3)*.08;return fixed4(_Color.rgb*(light+rim),1);}
            ENDCG
        }
    }
}
