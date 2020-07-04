# Unity PPC Shaders
Packed PBR Container shaders ___for Unity 2019.2+___ is a set of metalrough workflow PBR shaders that takes advantage of [Normal Map Swizzling](http://wiki.polycount.com/wiki/Normal_Map_Compression#DXT5nm_Compression) and [Channel Packing](http://wiki.polycount.com/wiki/ChannelPacking), while reducing sampler call count. Technical details below.

![Preview](https://i.imgur.com/OaLR2sR.png)

# Features
 * Albedo/Base Colour + Opacity input node with optional multiplicative tinter/opacity slider
 * PPC input node with metalness and roughness contribution sliders
   * *Optional* Roughness to Smoothness (glossines) adapter
 * *Optional* Ambient Occlusion input node with contribution slider
 * *Optional* Self-Illumination (emission) node with tinter
   * *Optional* Self-Illumination Pack (mask + pan) input node
   * Panner
   * *Optional* Sine Wave Oscillator
     * Amplitude multiplier
     * Frequency multiplier
     * Radial/Direct switch
     * *Optional* Peak clamper
       * Lower and Upper peak clamp values
 * *Optional* Fast Approximate Subsurface Scattering (rgb) + tinter
   * *Optional* FASS Map inverter
   * Contribution power
   * Normal direction distortion
   * Scattering (distance) falloff,
   * Direct light contribution multiplier
   * Indirect light contribution multiplier
   * Shadow negation multiplier

It comes with Opaque, AlphaBlend, Premultiplied Alpha, and AlphaTest variations, just like unity's Standard, except it is split into separate shaders to simplify editor's code a bit.

# Installation
Simply [grab the latest release](https://github.com/Rikketh/Unity-PPC-Shaders/releases/latest) or download the repo, then throw `Shaders` folder somewhere into your project's `Assets` folder. If everything's fine, you should be able to locate new shader entries under `Custom/Rickter/PPC/` path.

# Usage
Firts of all: these shaders were created to suit my PBR needs, and were somewhat influenced by Valve's Complex VR shader distributed with Half-Life: Alyx.

### PPC
This shader is only for PBR, and it expects to have a PPC input, which obviously has to be set in linear space most of the time. The PPC is a texture with four channels with following contents:

 * **Red** contains Roughness
 * **Green** contains tangent space normal's Y influence
 * **Blue** contains Metalness
 * **Alpha** contains tanget space normal's X influence

If you're using Substance Painter or similar software that's capable of generating OpenGL or DirectX normal maps, you can remap channels in the following manner:

 * Normal Map Red → PPC Alpha
 * Normal Map Green → PPC Green
 * Roughness Grey → PPC Red
 * Metalness Grey → PPC Blue

### SI Pack
In similar manner, SI Pack takes advantage of channel packing, albeit wasting one mandatory and one optional channel. I believe you can find use for Blue and Alpha channels yourself.

* **Red** contains multiplicative mask that's applied to SI/SI's map
* **Green** contains multiplicative horizontal or vertical pan map used for various smooth transitions
* **Blue** and **Alpha** are not used at the time.


# Technical Details
As mentioned above, PPC shader takes advantage of swizzling and channel packing to implement single-sample PBR+NM map. However, since the shader is built over unity's standard PBS shader, it utilises prebuilt lighting system with some minor tweaks to allow for FASS.

Once the texture is sampled, two of its channels are sent to `UnpackNormalPPC` method that creates a Virtual Object with normalized channels. However, since we are not supplying Z map with our normal map, we have to restore said tanget. Luckily for us, Z tangent never falls below 0, and it can be remediated at runtime using following formula: `z = √(1 - x² - y²)`, which translates into ShaderLab's `sqrt(1 - saturate(dot(normal.xy, normal.xy)))` (.xy has to be normalized beforehand, of course).

Thus, we have succefully restored a missing channel exactly how every other fucking engine does it with DXT5nm compression (including Unity itself), while condensing 3 different maps into a single map. Yay. Now we just have to feed roughness and metalness to appropriate PBS slots.