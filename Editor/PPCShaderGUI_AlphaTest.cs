// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

using System;
using UnityEngine;

namespace UnityEditor
{
	internal class PPCShaderGUI_AlphaTest : ShaderGUI
	{
		private static class Styles
		{
			public static GUIContent cullText = new GUIContent("Cull", "Face Culling (Front, Back, Off)");

			public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Opacity (A), sRGB");
			public static string alphaTestCutoff = "Cutoff shift";

			public static GUIContent ppcMapText = new GUIContent("PPC", "Packed PBR Container (RGBA)");
			public static string metalnessText = "Metalness power";
			public static string roughnessText = "Roughness power";
			public static string roughnessInvertText = "Invert roughness";

			public static GUIContent aoEnabled = new GUIContent("Ambient Occlusion", "Add .Occlusion property to rendering stack");
			public static GUIContent aoText = new GUIContent("AO Map", "Occlusion (R)");

			public static GUIContent siMapTintText = new GUIContent("Self Illumination", "Selfillumination (RGB)");
			public static GUIContent siPackText = new GUIContent("SI Pack", "Mask (R), Pan (G), Attenuation (B)");

			public static string siPanSpeed = "Pan Speed";
			public static string siOscEnabled = "Oscillation";
			public static string siOscClampsEnabled = "Clamp Peaks";
			public static string siOscPower = "Oscillation Amplitude";
			public static string siOscSpeed = "Oscillation Frequency";
			public static string siOscMode = "Use radial waveform"; // (2 * pi / freq)
			public static string siOscClamps = "Oscillation Clamps";

			public static string ssEnabled = "FASS";
			public static GUIContent ssMap = new GUIContent("FASS Map", "Fast Approximate Subsurface Scattering (RGB)");
			public static string ssMapInvert = "Invert map";
			public static string ssPower = "Contribution power";
			public static string ssNormalDistortion = "Normal distortion";
			public static string ssScattering = "Scattering Falloff";
			public static string ssDirectContrib = "Direct contribution";
			public static string ssAmbientContrib = "Indirect contribution";
			public static string ssShadowContrib = "Shadow power";

			public static string primaryMapsText = "PBR Mapping";
			public static string advancedPropertiesText = "Advanced Properties";
		}

		MaterialProperty cullMode = null;

		MaterialProperty albedoMap, albedoTint = null;
		MaterialProperty alphaTestCutoff = null;

		MaterialProperty PPCMap = null;
		MaterialProperty metalnessPower, roughnessPower = null;
		MaterialProperty roughnessInvert = null;

		MaterialProperty aoEnabled, aoMap, aoPower = null;

		MaterialProperty siMap, siTint, siPack = null;
		MaterialProperty siPanSpeed, siOscEnabled, siOscClampsEnabled, siOscPower, siOscSpeed, siOscMode, siOscClamps = null;

		MaterialProperty ssEnabled, ssMap, ssMapInvert, ssTint, ssPower, ssNormalDistortion, ssScattering, ssDirectContrib, ssAmbientContrib, ssShadowContrib = null;

		MaterialEditor m_MaterialEditor;
		ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1 / 99f, 3f);

		bool m_FirstTimeApply = true;

		public void FindProperties(MaterialProperty[] props)
		{
			cullMode = FindProperty("_CullMode", props);

			albedoMap = FindProperty("_MainTex", props);
			albedoTint = FindProperty("_MainTexColor", props);
			alphaTestCutoff = FindProperty("_CutoffShift", props);

			PPCMap = FindProperty("_PPC", props);
			metalnessPower = FindProperty("_MetalPower", props, false);
			roughnessPower = FindProperty("_RoughnessPower", props, false);
			roughnessInvert = FindProperty("_RoughnessVsSmoothness", props, false);

			aoEnabled = FindProperty("_AmbientOcclusionEnabled", props);
			aoMap = FindProperty("_AmbientOcclusion", props, false);
			aoPower = FindProperty("_AmbientOcclusionPower", props, false);

			siMap = FindProperty("_EmissionMap", props);
			siPack = FindProperty("_EmissionPack", props, false);
			siTint = FindProperty("_EmissionColor", props, false);

			siPanSpeed = FindProperty("_EmissionPanSpeed", props, false);
			siOscEnabled = FindProperty("_EmissionOscillate", props);
			siOscClampsEnabled = FindProperty("_EmissionOscClampPeaks", props, false);
			siOscPower = FindProperty("_EmissionOscPower", props, false);
			siOscSpeed = FindProperty("_EmissionOscSpeed", props, false);
			siOscMode = FindProperty("_EmissionOscMode", props, false);
			siOscClamps = FindProperty("_EmissionOscClamp", props, false);

			ssEnabled = FindProperty("_TranslucencyEnabled", props);
			ssMap = FindProperty("_TranslucencyMap", props, false);
			ssMapInvert = FindProperty("_InvertSS", props, false);
			ssTint = FindProperty("_TranslucencyTint", props, false);
			ssPower = FindProperty("_TranslucencyStrength", props, false);
			ssNormalDistortion = FindProperty("_TransNormalDistortion", props, false);
			ssScattering = FindProperty("_TransScattering", props, false);
			ssDirectContrib = FindProperty("_TransDirect", props, false);
			ssAmbientContrib = FindProperty("_TransAmbient", props, false);
			ssShadowContrib = FindProperty("_TransShadow", props, false);

		}

		float GetInspectorWidth()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			Rect scale = GUILayoutUtility.GetLastRect();
			return scale.width;
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			// Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
			// material to a standard shader.
			// Do this before any GUI code has been issued to prevent layout issues in subsequent GUILayout statements (case 780071)
			if (m_FirstTimeApply)
			{
				MaterialChanged(material);
				m_FirstTimeApply = false;
			}

			ShaderPropertiesGUI(material);
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;

			// Detect any changes to the material
			EditorGUI.BeginChangeCheck();
			{
				GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);

				//m_MaterialEditor.TexturePropertyWithHDRColor(Styles.albedoText, albedoMap, albedoTint, m_ColorPickerHDRConfig, true);
				m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoTint);
				m_MaterialEditor.RangeProperty(alphaTestCutoff, Styles.alphaTestCutoff);

				EditorGUI.BeginChangeCheck();
				m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
				// Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
				if (EditorGUI.EndChangeCheck()) {
					siMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset;
				}

				EditorGUILayout.Space();

				m_MaterialEditor.TexturePropertySingleLine(Styles.ppcMapText, PPCMap);
				m_MaterialEditor.RangeProperty(metalnessPower, Styles.metalnessText);
				m_MaterialEditor.RangeProperty(roughnessPower, Styles.roughnessText);
				m_MaterialEditor.ShaderProperty(roughnessInvert, Styles.roughnessInvertText);

				EditorGUILayout.Space();

				m_MaterialEditor.ShaderProperty(aoEnabled, Styles.aoEnabled);
				if (aoEnabled.floatValue == 1) {
					m_MaterialEditor.TexturePropertySingleLine(Styles.aoText, aoMap, aoPower);
				}

				EditorGUILayout.Space();

				DoEmissionArea(material);

				EditorGUILayout.Space();

				DoFASSArea(material);

				EditorGUILayout.Space();
			}

			if (EditorGUI.EndChangeCheck())
			{
				MaterialChanged(material);
			}

			EditorGUILayout.Space();

			GUILayout.Label(Styles.advancedPropertiesText, EditorStyles.boldLabel);
			m_MaterialEditor.ShaderProperty(cullMode, Styles.cullText);
			m_MaterialEditor.EnableInstancingField();
			m_MaterialEditor.RenderQueueField();
		}

		void DoEmissionArea(Material material)
		{
			// Emission for GI?
			if (m_MaterialEditor.EmissionEnabledProperty())
			{
				bool hadEmissionTexture = siMap.textureValue != null;

				// Texture and HDR color controls
				m_MaterialEditor.TexturePropertyWithHDRColor(Styles.siMapTintText, siMap, siTint, m_ColorPickerHDRConfig, false);

				// If texture was assigned and color was black set color to white
				float brightness = siTint.colorValue.maxColorComponent;
				if (siMap.textureValue != null && !hadEmissionTexture && brightness <= 0f) {
					siTint.colorValue = Color.white;
				}

				// change the GI flag and fix it up with emissive as black if necessary
				m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);

				m_MaterialEditor.TexturePropertySingleLine(Styles.siPackText, siPack);

				m_MaterialEditor.ShaderProperty(siPanSpeed, Styles.siPanSpeed);

				// #### Oscillators #### //

				m_MaterialEditor.ShaderProperty(siOscEnabled, Styles.siOscEnabled);
				if (siOscEnabled.floatValue == 1) {
					m_MaterialEditor.RangeProperty(siOscPower, Styles.siOscPower);
					m_MaterialEditor.RangeProperty(siOscSpeed, Styles.siOscSpeed);
					m_MaterialEditor.ShaderProperty(siOscMode, Styles.siOscMode);

					m_MaterialEditor.ShaderProperty(siOscClampsEnabled, Styles.siOscClampsEnabled);
					if (siOscClampsEnabled.floatValue == 1) {
						m_MaterialEditor.ShaderProperty(siOscClamps, Styles.siOscClamps);
					}
				}
			}
		}

		void DoFASSArea(Material material)
		{
			m_MaterialEditor.ShaderProperty(ssEnabled, Styles.ssEnabled);
			if (ssEnabled.floatValue == 1)
			{
				bool hadSSTexture = ssMap.textureValue != null;

				// Texture and HDR color controls
				m_MaterialEditor.TexturePropertyWithHDRColor(Styles.ssMap, ssMap, ssTint, m_ColorPickerHDRConfig, false);

				// If texture was assigned and color was black set color to white
				float brightness = ssTint.colorValue.maxColorComponent;
				if (ssMap.textureValue != null && !hadSSTexture && brightness <= 0f) {
					ssTint.colorValue = Color.white;
				}

				m_MaterialEditor.ShaderProperty(ssMapInvert, Styles.ssMapInvert);
				m_MaterialEditor.RangeProperty(ssPower, Styles.ssPower);
				m_MaterialEditor.RangeProperty(ssNormalDistortion, Styles.ssNormalDistortion);
				m_MaterialEditor.RangeProperty(ssScattering, Styles.ssScattering);
				m_MaterialEditor.RangeProperty(ssDirectContrib, Styles.ssDirectContrib);
				m_MaterialEditor.RangeProperty(ssAmbientContrib, Styles.ssAmbientContrib);
				m_MaterialEditor.RangeProperty(ssShadowContrib, Styles.ssShadowContrib);
			}
		}

		static void SetMaterialKeywords(Material material)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation

			// A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
			// or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
			// The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
			MaterialEditor.FixupEmissiveFlag(material);
			bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
			SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

			bool shouldAmbientOcclusionBeEnabled = material.GetInt("_AmbientOcclusionEnabled") == 1;
			SetKeyword(material, "PPC_AO_ENABLED", shouldAmbientOcclusionBeEnabled);

			bool shouldOscillatorBeEnabled = material.GetInt("_EmissionOscillate") == 1;
			SetKeyword(material, "PPC_EMISSION_OSCILLATOR_ENABLED", shouldOscillatorBeEnabled);
		}

		static void MaterialChanged(Material material)
		{
			SetMaterialKeywords(material);
		}

		static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}
	}
} // namespace UnityEditor
