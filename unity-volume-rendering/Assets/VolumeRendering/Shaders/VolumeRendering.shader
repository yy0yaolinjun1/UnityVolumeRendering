Shader "VolumeRendering/VolumeRendering"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Volume("Volume", 3D) = "" {}
		_Intensity("Intensity", Range(0.0, 5.0)) = 1.2
		_Threshold("Threshold", Range(0.0, 1.0)) = 0.95
		_SliceMin("Slice min", Vector) = (0.0, 0.0, 0.0, -1.0)
		_SliceMax("Slice max", Vector) = (1.0, 1.0, 1.0, -1.0)
		_Diffuse("Diffuse", Color) = (0.6, 0.6, 0.6, 0.3)
		_Specular("Specular", Color) = (0.2, 0.2, 0.2, 1)
		_Gloss("Gloss", Range(8.0, 256)) = 17
		
		//_ColorArray("Color")
		
	}


		CGINCLUDE

			ENDCG

			SubShader{
				Cull Back
				Blend SrcAlpha OneMinusSrcAlpha
				 ZTest Always

				Pass
				{
					CGPROGRAM
              #ifndef ITERATIONS
			  #define ITERATIONS 1024
					
					#pragma vertex vert
					#pragma fragment frag
                    #pragma exclude_renderers d3d11_9x
		#ifndef __VOLUME_RENDERING_INCLUDED__
		#define __VOLUME_RENDERING_INCLUDED__

		#include "UnityCG.cginc"
        #include "Lighting.cginc"
		#endif

        #define P 25.0
        #define DELTA 0.01

		half4 _Color;
	    half4 transferFunc[256];
		sampler3D _Volume;
		sampler2D TransferTex;

		uniform half4 _ColorArray[256];
		half _Intensity, _Threshold;
		half3 _SliceMin, _SliceMax;
		float4x4 _AxisRotationMatrix;
	
		half3 SampleDist=.5f;
		half ActualSampleDist = .5f;
		half BaseSampleDist = .5f;
		

		//light
		half4 _Diffuse;
		half4 _Specular;

		half _Gloss;

		struct Ray {
		  float3 origin;
		  float3 dir;
		};

		struct AABB {
		  float3 min;
		  float3 max;
		};


		
		bool intersect(Ray r, AABB aabb, out float t0, out float t1)
		{
		  float3 invR = 1.0 / r.dir;
		  float3 tbot = invR * (aabb.min - r.origin);
		  float3 ttop = invR * (aabb.max - r.origin);
		  float3 tmin = min(ttop, tbot);
		  float3 tmax = max(ttop, tbot);
		  float2 t = max(tmin.xx, tmin.yz);
		  t0 = max(t.x, t.y);
		  t = min(tmax.xx, tmax.yz);
		  t1 = min(t.x, t.y);
		  return t0 <= t1;
		}
	
		float3 localize(float3 p) {
		  return mul(unity_WorldToObject, float4(p, 1)).xyz;
		}

		float3 get_uvw(float3 p) {
		  return (p + 0.5);
		}

		float sample_volume(float3 uvw, float3 p)
		{
		  float v = tex3D(_Volume, uvw).r * _Intensity;
		  return v;
		}

		bool outside(float3 uvw)
		{
		  const float EPSILON = 0.01;
		  float lower = -EPSILON;
		  float upper = 1 + EPSILON;
		  return (
					uvw.x < lower || uvw.y < lower || uvw.z < lower ||
					uvw.x > upper || uvw.y > upper || uvw.z > upper
				);
		}

		struct appdata
		{
		  float4 vertex : POSITION;
		  float2 uvw : TEXCOORD0;

		  float3 normal : NORMAL;
		};

		struct v2f
		{
		  float4 vertex : SV_POSITION;
		  float2 uvw : TEXCOORD0;
		  float3 world : TEXCOORD1;

		  float3 worldNormal: TEXCOORD2;
		};

		v2f vert(appdata v)
		{
		  v2f o;
		  o.vertex = UnityObjectToClipPos(v.vertex);
		  o.uvw = v.uvw;
		  o.world = mul(unity_ObjectToWorld, v.vertex).xyz;

		  o.worldNormal= UnityObjectToWorldNormal(v.normal);
		  return o;
		}

		
		float4 shading(float3 N, float3 V, float3 L)
		{
			float3 Ka = float3(0.1, 0.1, 0.1); 
			float3 Kd = float3(0.6, 0.6, 0.6);
			float3 Ks = float3(0.2, 0.2, 0.2);
			float shininess = 100.0;

			float3 lightColor = float3(1.0, 1.0, 1.0);
			float3 ambientLight = float3(0.3, 0.3, 0.3);

			float3 H = normalize(L + V);
			// Compute ambient term
			float3 ambient = Ka * ambientLight;

			// Compute the diffuse term
			float diffuseLight = max(dot(L, N), 0);
			float3 diffuse = Kd * lightColor * diffuseLight;

			// Compute the specular term
			float specularLight = pow(max(dot(H, N), 0), shininess);
			if (diffuseLight <= 0) specularLight = 0;
			float3 specular = Ks * lightColor * specularLight;
			return float4(ambient + diffuse + specular,1.0f);
		}

		fixed4 frag(v2f i) : SV_Target
		{
		  fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
		  Ray ray;
		  ray.origin = localize(i.world);

		  fixed3 worldLightDir = -normalize(UnityWorldSpaceLightDir(i.world));
	


		  fixed3 worldNormal = normalize(i.worldNormal);

		  float3 dir = normalize(i.world - _WorldSpaceCameraPos);


		  fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.world.xyz);
		  fixed3 halfDir = normalize(worldLightDir + viewDir);

		//fade shading
		  fixed3 L = fixed3(1, 1, 1);
		  
		  ray.dir = normalize(mul((float3x3) unity_WorldToObject, dir));

		 // ComputeTransferFunction();
		  AABB aabb;
		  aabb.min = float3(-0.5, -0.5, -0.5);
		  aabb.max = float3(0.5, 0.5, 0.5);

		  float tnear;
		  float tfar;
		  intersect(ray, aabb, tnear, tfar);

		 //CalculateColorSpline

		  tnear = max(0.0, tnear);

		  float3 start = ray.origin;

		  float3 end = ray.origin + ray.dir * tfar;

		  float dist = abs(tfar - tnear); // float dist = distance(start, end);

		  float step_size = dist / float(ITERATIONS);


		  float3 step = normalize(end - start) * step_size;

		  float4 dst = float4(0, 0, 0, 0);
		  float3 p = start;


		  float4 sv = 0;
		  float4 value = 0;
		  int index = 0;


		  fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(worldNormal, worldLightDir));

		  //float diffuse = dot(worldLightDir, worldNormal)*0.7 + 0.3;
		  fixed3 specular= _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(worldNormal, halfDir)), _Gloss);

		  [loop]
		  for (int iter = 0; iter < ITERATIONS; iter++)
		  {
			
			float3 uvw = p + 0.5;//*direction;

			//float v = tex3D(_Volume, uvw) * _Intensity;
		     
			sv = tex3D(_Volume, uvw)*_Intensity;
			 
			value = tex3D(_Volume, uvw);

				float3 sample1, sample2;
				// six texture samples for the gradient
				sample1.x = tex3D(_Volume, uvw - half3(DELTA, 0.0, 0.0)).x;
				sample2.x = tex3D(_Volume, uvw + half3(DELTA, 0.0, 0.0)).x;
				sample1.y = tex3D(_Volume, uvw - half3(0.0, DELTA, 0.0)).y;
				sample2.y = tex3D(_Volume, uvw + half3(0.0, DELTA, 0.0)).y;
				sample1.z = tex3D(_Volume, uvw - half3(0.0, 0.0, DELTA)).z;
				sample2.z = tex3D(_Volume, uvw + half3(0.0, 0.0, DELTA)).z;

				// central difference and normalization
				float3 N = normalize(sample2 - sample1);
				// calculate light- and viewing direction
				float3 L = worldLightDir;

				float3 V = viewDir;
				// add local illumination

			

			    //fade shading
			    float fadeShading = dot(sv.xyz, L);

			    index = int(value.r*255);
			    float4 src = (float4)sv;

			

			    src.r = _ColorArray[index].x;
			    src.g = _ColorArray[index].y;
			    src.b = _ColorArray[index].z;
		        src.a = _ColorArray[index].w;

			    src.rgb += fadeShading *shading(N, V, L)*_Intensity;

		        //src.a *= 0.8;

			    src.rgb *= src.a;

			    dst = (1.0 - dst.a) * src + dst;
			
			//ds = normalize(end - start) * step_size;
			p.xyz += step;

			if (dst.a > _Threshold)
			{
			  break;
			}
		  }
		  half4 cor = half4(specular, 1);
		  half4 dif = half4(diffuse,0.1);
		  return saturate(dst);

		}

		#endif 
					ENDCG
				}
		}
}
