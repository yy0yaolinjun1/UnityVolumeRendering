
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace VolumeRendering
{

    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class VolumeRendering : MonoBehaviour {
        #region Variable
        [SerializeField] protected Shader shader;
        protected Material material;

        [SerializeField] Color color = Color.white;
        [SerializeField] Color specular = Color.white;
        [SerializeField] Color diffuse = Color.white;
        [Range(0f, 1f)] public float threshold = 0.5f;
        [Range(0f, 5f)] public float intensity = 1.5f;
        [Range(0f, 1f)] public float alphaValueThreadshold = 0.1f;
        [SerializeField] Color[] colorarray=new Color[256];
        public Quaternion axis = Quaternion.identity;

        public Texture3D volume;
        

        //**********
        private Texture2D tex2;
        private Texture3D tex3;

        private Vector3[] mGradients;
        private Vector4[] mScalars;
        private Vector4[] gradients;

        private int mWidth;
        private int mHeight;
        private int mDepth;

        string m_Path;
     
        private List<TransferControlPoint> mColorKnots = new List<TransferControlPoint> {
                                    new TransferControlPoint(.91f, .7f, .61f, 0),   //color for skin
                                    new TransferControlPoint(.91f, .7f, .61f, 80),  //color for skin
                                    new TransferControlPoint(0.9f, 0.85f, 0.7f, 82), //color for bone
                                    new TransferControlPoint(1f, 0.95f, 0.8f, 256) //color for bone
                                    };
        private List<TransferControlPoint> mAlphaKnots = new List<TransferControlPoint> {
                                    new TransferControlPoint(0.0f, 0),
                                    new TransferControlPoint(0.0f, 40),
                                    new TransferControlPoint(0.0f, 60),
                                    new TransferControlPoint(0.09f, 63),
                                    new TransferControlPoint(0.0f, 80),
                                    new TransferControlPoint(0.9f, 82),
                                    new TransferControlPoint(1.0f, 256)
                                    };

        #endregion
        #region Class
        public class TransferControlPoint
        {
            public Vector4 Color;
            public int IsoValue;

            /// <summary>
            /// Constructor for color control points. 
            /// Takes rgb color components that specify the color at the supplied isovalue.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="z"></param>
            /// <param name="isovalue"></param>
            public TransferControlPoint(float r, float g, float b, int isovalue)
            {
                Color.x = r;
                Color.y = g;
                Color.z = b;
                Color.w = 1.0f;
                IsoValue = isovalue;
            }

            /// <summary>
            /// Constructor for alpha control points. 
            /// Takes an alpha that specifies the aplpha at the supplied isovalue.
            /// </summary>
            /// <param name="alpha"></param>
            /// <param name="isovalue"></param>
            public TransferControlPoint(float alpha, int isovalue)
            {
                Color.x = 0.0f;
                Color.y = 0.0f;
                Color.z = 0.0f;
                Color.w = alpha;
                IsoValue = isovalue;
            }
        }
        public class Cubic
        {
         
            private Vector4 a, b, c, d; // a + b*s + c*s^2 +d*s^3 

            public Cubic(Vector4 a, Vector4 b, Vector4 c, Vector4 d)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
            }
            public Color color;
            public int IsoValue;

            public Cubic()
            {
                color = new Color(0, 0, 0, 0);
                IsoValue = 0;
            }
            public static Cubic[] CalculateCubicSpline(int n, List<TransferControlPoint> v)
            {
                Vector4[] gamma = new Vector4[n + 1 ];
                Vector4[] delta = new Vector4[n + 1];
                Vector4[] D = new Vector4[n + 1];

                int i;
                /* We need to solve the equation
                 * taken from: http://mathworld.wolfram.com/CubicSpline.html
                   [2 1       ] [D[0]]   [3(v[1] - v[0])  ]
                   |1 4 1     | |D[1]|   |3(v[2] - v[0])  |
                   |  1 4 1   | | .  | = |      .         |
                   |    ..... | | .  |   |      .         |
                   |     1 4 1| | .  |   |3(v[n] - v[n-2])|
                   [       1 2] [D[n]]   [3(v[n] - v[n-1])]

                   by converting the matrix to upper triangular.
                   The D[i] are the derivatives at the control points.
                 */

                //this builds the coefficients of the left matrix
                gamma[0] = Vector4.zero;
                gamma[0].x = 1.0f / 2.0f;
                gamma[0].y = 1.0f / 2.0f;
                gamma[0].z = 1.0f / 2.0f;
                gamma[0].w = 1.0f / 2.0f;
                Vector4 cur = Vector4.zero;

                for (i = 1; i < n; i++)
                {
                    cur = ((4 * Vector4.one) - gamma[i - 1]);
                    gamma[i] = new Vector4(1 / cur.x, 1 / cur.y, 1 / cur.z, 1 / cur.w);
                }
                cur = ((2 * Vector4.one) - gamma[n - 1]);
                gamma[n] = new Vector4(1 / cur.x, 1 / cur.y, 1 / cur.z, 1 / cur.w);


                cur = 3 * (v[1].Color - v[0].Color);
                delta[0] = new Vector4(cur.x * gamma[0].x, cur.y * gamma[0].y, cur.z * gamma[0].z, cur.w * gamma[0].w);

                for (i = 1; i < n; i++)
                {
                    cur = 3 * (v[i + 1].Color - v[i - 1].Color) - delta[i - 1];
                    delta[i] = new Vector4(cur.x * gamma[i].x, cur.y * gamma[i].y, cur.z * gamma[i].z, cur.w * gamma[i].w);
                }
                cur = 3 * (v[n].Color - v[n - 1].Color) - delta[n - 1];
                delta[n] = new Vector4(cur.x * gamma[n].x, cur.y * gamma[n].y, cur.z * gamma[n].z, cur.w * gamma[n].w);

                D[n] = delta[n];

                for (i = n - 1; i >= 0; i--)
                {
                    cur = new Vector4(gamma[i].x * D[i + 1].x, gamma[i].y * D[i + 1].y, gamma[i].z * D[i + 1].z, gamma[i].w * D[i + 1].w);
                    D[i] = delta[i] - cur;
                }
                Cubic[] C = new Cubic[n];
                for (i = 0; i < n; i++)
                {
                    C[i] = new Cubic(v[i].Color, D[i], 3 * (v[i + 1].Color - v[i].Color) - 2 * D[i] - D[i + 1],
                             2 * (v[i].Color - v[i + 1].Color) + D[i] + D[i + 1]);
                }
                return C;
            }
            public Vector4 GetPointOnSpline(float s)
            {
                return (((d * s) + c) * s + b) * s + a;
            }
        }
        #endregion
        #region Method




        public void ComputeTransferFunction()
        {
            Vector4[] transferFunc = new Vector4[256];
            Cubic[] colorCubic = Cubic.CalculateCubicSpline(mColorKnots.Count - 1, mColorKnots);
            Cubic[] alphaCubic = Cubic.CalculateCubicSpline(mAlphaKnots.Count - 1, mAlphaKnots);

            int numTF = 0;
            for (int i = 0; i < mColorKnots.Count - 1; i++)
            {
                int steps = mColorKnots[i + 1].IsoValue - mColorKnots[i].IsoValue;

                for (int j = 0; j < steps; j++)
                {
                    float k = (float)j / (float)(steps - 1);

                    transferFunc[numTF++] = colorCubic[i].GetPointOnSpline(k);
                }
            }

            numTF = 0;
            for (int i = 0; i < mAlphaKnots.Count - 1; i++)
            {
                int steps = mAlphaKnots[i + 1].IsoValue - mAlphaKnots[i].IsoValue;

                for (int j = 0; j < steps; j++)
                {
                    float k = (float)j / (float)(steps - 1);

                    transferFunc[numTF++].w = alphaCubic[i].GetPointOnSpline(k).w;
                }
            }
            for (int i = 0; i < colorarray.Length; i++)
            {
               // Debug.Log(i+" "+transferFunc[i]);
               if(transferFunc[i].w>1)
                {
                    transferFunc[i].w = 1;
                }
               if(transferFunc[i].w<alphaValueThreadshold)
                {
                    transferFunc[i].w = 0;
                }
                colorarray[i] = transferFunc[i];
                //Debug.Log(i + " " + transferFunc[i]);
            }
          //  material.SetColorArray("_ColorArray", colorarray);

        }

      
        void Constrain(ref float min, ref float max)
        {
            const float threshold = 0.025f;
            if (min > max - threshold)
            {
                min = max - threshold;
            }
            else if (max < min + threshold)
            {
                max = min + threshold;
            }
        }
        Mesh Build()
        {
            var vertices = new Vector3[] {
                new Vector3 (-0.5f, -0.5f, -0.5f),
                new Vector3 ( 0.5f, -0.5f, -0.5f),
                new Vector3 ( 0.5f,  0.5f, -0.5f),
                new Vector3 (-0.5f,  0.5f, -0.5f),
                new Vector3 (-0.5f,  0.5f,  0.5f),
                new Vector3 ( 0.5f,  0.5f,  0.5f),
                new Vector3 ( 0.5f, -0.5f,  0.5f),
                new Vector3 (-0.5f, -0.5f,  0.5f),
            };
            var triangles = new int[] {
                0, 2, 1,
                0, 3, 2,
                2, 3, 4,
                2, 4, 5,
                1, 2, 5,
                1, 5, 6,
                0, 7, 4,
                0, 4, 3,
                5, 4, 7,
                5, 7, 6,
                0, 6, 7,
                0, 1, 6
            };

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
        #endregion
        protected void Start()
        {
            //********
            material = new Material(shader);
            GetComponent<MeshFilter>().sharedMesh = Build();
            GetComponent<MeshRenderer>().sharedMaterial = material;
            ComputeTransferFunction();
            //loadRAWFile(Application.dataPath + "/VolumeRendering/Volumes/VisMale.raw");//not used yet

            //Debug.Log("Path: " + Application.dataPath + "/VolumeRendering/Volumes/VisMale.raw");//not used yet


            Debug.Log(volume.width);
            Debug.Log("weight height depth" + volume.width + "  " + volume.height + "  " + volume.depth);
        }
        protected void Update()
        {
            material.SetTexture("_Volume", volume);
            material.SetColor("_Color", color);
            material.SetFloat("_Threshold", threshold);
            material.SetColor("_Specular", specular);
            material.SetColor("_Diffuse", diffuse);
            material.SetFloat("_Intensity", intensity);
            material.SetMatrix("_AxisRotationMatrix", Matrix4x4.Rotate(axis));
            material.SetColorArray("_ColorArray", colorarray);
            material.SetFloat("_alphaValueThreadshold ", alphaValueThreadshold);

        }

       


        void OnDestroy()
        {
            Destroy(material);
        }
        //not used yet
        #region the code not used yet
        private void loadRAWFile(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.Open);
            long length = file.Length;

            if (length > mWidth * mHeight * mDepth)
            {
                loadRAWFile16(file);
            }
            else
            {
                loadRAWFile8(file);
            }

            file.Close();
        }
        /// <summary>
        /// Loads an 8-bit RAW file.
        /// </summary>
        /// <param name="file"></param>
        
        //not used yet
        private void loadRAWFile8(FileStream file)
        {
            BinaryReader reader = new BinaryReader(file);
            if (reader == null)
            {
                Debug.Log("FILE PATH IS NOT VALID");
                return;
            }
            byte[] buffer = new byte[mWidth * mHeight * mDepth];

            Debug.Log("xxxxx" + mWidth);
            int size = sizeof(byte);

            reader.Read(buffer, 0, size * buffer.Length);

            reader.Close();

            //scale the scalar values to [0, 1]
            mScalars = new Vector4[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                mScalars[i] = new Vector4(0, 0, 0, (float)buffer[i] / byte.MaxValue);
            }

            //mVolume.SetData(mScalars);
            //mEffect.Parameters["Volume"].SetValue(mVolume);
        }

        /// <summary>
        /// Loads a 16-bit RAW file.
        /// </summary>
        /// <param name="file"></param>
        private void loadRAWFile16(FileStream file)
        {
            BinaryReader reader = new BinaryReader(file);
            if (reader == null)
            {
                Debug.Log("FILE PATH IS NOT VALID");
                return;
            }
            ushort[] buffer = new ushort[mWidth * mHeight * mDepth];

            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = reader.ReadUInt16();

            reader.Close();

            //scale the scalar values to [0, 1]
            mScalars = new Vector4[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                mScalars[i] = new Vector4(0, 0, 0, (float)buffer[i] / ushort.MaxValue);
            }
           
            //mVolume.SetData(mScalars);
            //mEffect.Parameters["Volume"].SetValue(mVolume);
        }
        public void loadGradients()
        { 
            //generate the gradient of the volume,parameter is sampleSize
            GenerateGradient(1);

            //filter the gradient with 3X3X3 filter
            filterNxNxN(3);

            for (int i = 0; i < mGradients.Length; i++)
            {
                gradients[i] = new Vector4(mGradients[i].x, mGradients[i].y, mGradients[i].z, mScalars[i].w);
            }
            for(int j=0;j<100;j++)
            {
                Debug.Log(j + "gradient is" + gradients[j]);
            }
            //TODO mVolume.SetData<HalfVector4>(gradients);
        }
        //not used yet
        public void GenerateGradient(int sampleSize)
        {
            int n = sampleSize;
            Vector3 normal = Vector3.zero;
            Vector3 s1, s2;

            int index = 0;
            for (int z = 0; z <mDepth; z++)
            {
                for (int y = 0; y <mHeight; y++)
                {
                    for (int x = 0; x < mWidth; x++)
                    {
                        s1.x = sampleVolume(x - n, y, z);
                        s2.x = sampleVolume(x + n, y, z);
                        s1.y = sampleVolume(x, y - n, z);
                        s2.y = sampleVolume(x, y + n, z);
                        s1.z = sampleVolume(x, y, z - n);
                        s2.z = sampleVolume(x, y, z + n);

                        mGradients[index++] = Vector3.Normalize(s2 - s1);
                        if (float.IsNaN(mGradients[index - 1].x))
                        {
                            mGradients[index - 1] = Vector3.zero;
                        }
                    }
                }
            }
        }
        //not used yet
        private float sampleVolume(int x, int y, int z)
        {
            x = (int)Mathf.Clamp(x, 0, volume.width - 1);
            y = (int)Mathf.Clamp(y, 0, volume.height - 1);
            z = (int)Mathf.Clamp(z, 0, volume.depth - 1);
            //???
            return (float)mScalars[x + (y * mWidth) + (z * mWidth * mHeight)].w;
        }
        //not used yet
        //filter the gradients
        private void filterNxNxN(int n)
        {
            int index = 0;
            for (int z = 0; z < mDepth; z++)
            {
                for (int y = 0; y < mHeight; y++)
                {
                    for (int x = 0; x < mWidth; x++)
                    {
                        mGradients[index++] = sampleNxNxN(x, y, z, n);
                    }
                }
            }
        }
        //not used yet
        //Samples the sub-volume graident volume and returns the average.
        private Vector3 sampleNxNxN(int x, int y, int z, int n)
        {
            n = (n - 1) / 2;

            Vector3 average = Vector3.zero;
            int num = 0;

            for (int k = z - n; k <= z + n; k++)
            {
                for (int j = y - n; j <= y + n; j++)
                {
                    for (int i = x - n; i <= x + n; i++)
                    {
                        if (isInBounds(i, j, k))
                        {
                            average += sampleGradients(i, j, k);
                            num++;
                        }
                    }
                }
            }

            average /= (float)num;
            if (average.x != 0.0f && average.y != 0.0f && average.z != 0.0f)
            {
                average.Normalize();
            }

            return average;
        }

        //not used yet
        // Checks whether the input is in the bounds of the volume data array
        private bool isInBounds(int x, int y, int z)
        {
            return ((x >= 0 && x < mWidth) &&
                    (y >= 0 && y < mHeight) &&
                    (z >= 0 && z < mDepth));
        }
        //sample the gradient volume value
        //not used yet
        private Vector3 sampleGradients(int x, int y, int z)
        {
            return mGradients[x + (y * mWidth) + (z * mWidth * mHeight)];
        }
        #endregion



    }

}


