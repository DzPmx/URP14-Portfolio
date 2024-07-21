## 作品集构成

 **1. PostProcess System:** 


简介：使用一个Renderfeature和RenderPass统一管理所有全屏后处理效果  后处理效果的激活和调整参数汇入Volueme

后处理内容：

Blur： Gaussian Blur、Box Blur、Kawase Blur、DualKawase Blur、Bokeh Blur、Tile Shift Blur、Iris Blur、Grainy Blur、Directional Blur、Radial Blur

Glitch：RGB Split、Image Block、Line Block、Tile Jitter、Scanline Jitter、Digital Stripe、Analog Noise、Wave Jitter


 **2.Reflection**

Planner Reflection：效果预览

Plannar Reflection Blur：使用Renderfeature对反摄相机渲染做后处理模糊 模糊方法：Dual Kawase Blur

 
 **3. Separable Subsurface Scattering**
 
Pre-Intergrated LUT Generator 
 
Pre-Intergrated Subsurface Scattering 

Spherical Gaussian Subsurface Scattering 
 
Separable Subsurface Scattering

Burley Normalized Subsurface Scattering 
