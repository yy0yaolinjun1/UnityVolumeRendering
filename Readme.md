# unity_volume_rendering
=====================

## This is a simple volume rendering project(including transferFunction and shading) implemented with unity2018.1.5.f1
## tested on Unity 2018.1.5, windows10,GTX 1070.

## reference the code below:
## 1 https://github.com/mattatz/unity-volume-rendering
## 2 http://graphicsrunner.blogspot.com/2009/01/volume-rendering-102-transfer-functions.html
## 3 the book "Real-Time Volume Graphics"

### screenshot 1
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/14.PNG">
### You can alter the value of the script"Rotate the Volume"to rotate the volume
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/15Rotate.PNG">
### You can slide the toggle "Intensity" to alter the intensity of light
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/16.PNG">
### You can edit the transferPoint in script"VolumeRendering.cs"to alter the Color and Alpha of the skin and bone.
### Beside,you can alter the value"Alpha Value Threadshold"in the script"VolumeRendering.cs"to display the different tissues 
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/17alpha.PNG">
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/18transferFunction.PNG">
### the image below is after fade shading(you can remove this effect by removing the 'fadeShading' in the code below
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/20fadeShading.PNG">
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/21fadeshading.PNG">

### if you try to display other RAW data,you should convert the raw file type to the Texture3d first
### you can use "VolumeAssetBuilder"(reference the code from https://github.com/mattatz/unity-volume-rendering)to convert the file type
<img src="https://github.com/yy0yaolinjun1/ScreenShot/blob/master/VolumeRendering/19AssetBuddle.PNG">
