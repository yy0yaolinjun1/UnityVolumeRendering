using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace VolumeRendering
{

    public class VolumeRenderingController : MonoBehaviour {

        [SerializeField] protected VolumeRendering volume;
        [SerializeField] protected Slider sliderXMin, sliderXMax, sliderYMin, sliderYMax, sliderZMin, sliderZMax,sliderIsovalue;
        [SerializeField] protected Transform axis;

        void Start ()
        {
            const float threshold = 0.025f;
           
        }

        void Update()
        {
            volume.axis = axis.rotation;
        }

        public void OnIntensity(float v)
        {
            volume.intensity = v;
        }

        public void OnThreshold(float v)
        {
            volume.threshold = v;
        }
        public void OnIsovalue(float v)
        {
            volume.alphaValueThreadshold = sliderIsovalue.value;

        }

    }

}


