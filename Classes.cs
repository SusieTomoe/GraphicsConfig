using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsConfig.Classes
{
    public class GraphicalConfiguration
    {
        /// <summary>
        /// UI Resolution Settings -> UI Resolution ※Requires reboot.
        /// </summary>
        public uint UiAssetType { get; set; }
        /// <summary>
        /// Resolution -> Graphics Upscaling
        /// </summary>
        public uint GraphicsRezoUpscaleType { get; set; }

        /// <summary>
        /// Resolution -> 3D Resolution Scaling (50 - 100)
        /// </summary>
        public uint GraphicsRezoScale { get; set; }

        /// <summary>
        /// Resolution -> Enable Dynamic Resolution
        /// </summary>
        public uint DynamicRezoType { get; set; }

        /// <summary>
        /// Resolution -> Frame Rate Threshold
        /// </summary>
        public uint DynamicRezoThreshold { get; set; }
        /// <summary>
        /// General -> Use low-detail models on distant objects. (LOD)
        /// </summary>
        public uint LodType_DX11 { get; set; }
        /// <summary>
        /// General -> Enable dynamic grass interaction.
        /// </summary>
        public uint GrassEnableDynamicInterference { get; set; }
        /// <summary>
        /// General -> Real-time Reflections
        /// </summary>
        public uint ReflectionType_DX11 { get; set; }
        /// <summary>
        /// General -> Edge Smoothing (Anti-aliasing)
        /// </summary>
        public uint AntiAliasing_DX11 { get; set; }
        /// <summary>
        /// General -> Transparent Lighting Quality
        /// </summary>
        public uint TranslucentQuality_DX11 { get; set; }
        /// <summary>
        /// General -> Grass Quality
        /// </summary>
        public uint GrassQuality_DX11 { get; set; }
        /// <summary>
        /// General -> Parallax Occlusion
        /// </summary>
        public uint ParallaxOcclusion_DX11 { get; set; }
        /// <summary>
        /// General -> Tessellation
        /// </summary>
        public uint Tessellation_DX11 { get; set; }
        /// <summary>
        /// General -> Glare
        /// </summary>
        public uint GlareRepresentation_DX11 { get; set; }
        /// <summary>
        /// Shadows -> Self
        /// </summary>
        public uint ShadowVisibilityTypeSelf_DX11 { get; set; }
        /// <summary>
        /// Shadows -> Party Members
        /// </summary>
        public uint ShadowVisibilityTypeParty_DX11 { get; set; }
        /// <summary>
        /// Shadows -> Other NPCs
        /// </summary>
        public uint ShadowVisibilityTypeOther_DX11 { get; set; }
        /// <summary>
        /// Shadows -> Enemies
        /// </summary>
        public uint ShadowVisibilityTypeEnemy_DX11 { get; set; }
        /// <summary>
        /// Shadow Quality -> Use low-detail models on shadows. (LOD)
        /// </summary>
        public uint ShadowLOD_DX11 { get; set; }
        /// <summary>
        /// Shadow Quality -> Use low-detail models on distant object shadows. (LOD)
        /// </summary>
        public uint ShadowBgLOD { get; set; }
        /// <summary>
        /// Shadow Quality -> Shadow Resolution
        /// </summary>
        public uint ShadowTextureSizeType_DX11 { get; set; }
        /// <summary>
        /// Shadow Quality -> Shadow Cascading
        /// </summary>
        public uint ShadowCascadeCountType_DX11 { get; set; }
        /// <summary>
        /// Shadow Quality -> Shadow Softening
        /// </summary>
        public uint ShadowSoftShadowType_DX11 { get; set; }
        /// <summary>
        /// Shadow Quality -> Cast Shadows
        /// </summary>
        public uint ShadowLightValidType { get; set; }
        /// <summary>
        /// Texture Detail -> Texture Resolution ※Requires reboot.
        /// </summary>
        public uint TextureRezoType { get; set; }
        /// <summary>
        /// Texture Detail -> Anisotropic Filtering
        /// </summary>
        public uint TextureAnisotropicQuality_DX11 { get; set; }
        /// <summary>
        /// Movement Physics -> Self
        /// </summary>
        public uint PhysicsTypeSelf_DX11 { get; set; }
        /// <summary>
        /// Movement Physics -> Party Members
        /// </summary>
        public uint PhysicsTypeParty_DX11 { get; set; }
        /// <summary>
        /// Movement Physics -> Other NPCs
        /// </summary>
        public uint PhysicsTypeOther_DX11 { get; set; }
        /// <summary>
        /// Movement Physics -> Enemies
        /// </summary>
        public uint PhysicsTypeEnemy_DX11 { get; set; }
        /// <summary>
        /// Effects -> Naturally darken the edges of the screen. (Limb Darkening)
        /// </summary>
        public uint Vignetting_DX11 { get; set; }
        /// <summary>
        /// Effects -> Blur the graphics around an object in motion. (Radial Blur)
        /// </summary>
        public uint RadialBlur_DX11 { get; set; }
        /// <summary>
        /// Effects -> Screen Space Ambient Occlusion
        /// </summary>
        public uint SSAO_DX11 { get; set; }
        /// <summary>
        /// Effects -> Glare
        /// </summary>
        public uint Glare_DX11 { get; set; }
        /// <summary>
        /// Effects -> Water Reflection
        /// </summary>
        public uint DistortionWater_DX11 { get; set; }
        /// <summary>
        /// Cinematic Cutscenes -> Enable depth of field.
        /// </summary>
        public uint DepthOfField_DX11 { get; set; }
    }

    //public class GraphicalConfigurationStrings
    //{
    //    /// <summary>
    //    /// Resolution -> Graphics Upscaling
    //    /// </summary>
    //    public const string GraphicsRezoUpscaleType = "GraphicsRezoUpscaleType";

    //    /// <summary>
    //    /// Resolution -> 3D Resolution Scaling (50 - 100)
    //    /// </summary>
    //    public uint GraphicsRezoScale { get; set; }

    //    /// <summary>
    //    /// Resolution -> Enable Dynamic Resolution
    //    /// </summary>
    //    public uint DynamicRezoType { get; set; }

    //    /// <summary>
    //    /// Resolution -> Frame Rate Threshold
    //    /// </summary>
    //    public uint DynamicRezoThreshold { get; set; }

    //    //public DateTime LastShoutout { get; set; }
    //    //public DateTime LastTimeTheyGifted { get; set; }
    //}

    //public class SettingsToggle
    //{
    //    /// <summary>
    //    /// Indicates the setting is off
    //    /// </summary>
    //    public const uint Off = 0;

    //    /// <summary>
    //    /// Indicates the setting is on
    //    /// </summary>
    //    public const uint On = 1;
    //}

    //public class GraphicsUpscaling
    //{
    //    /// <summary>
    //    /// Indicates the setting is off
    //    /// </summary>
    //    public const uint AMD_FSR = 0;

    //    /// <summary>
    //    /// Indicates the setting is on
    //    /// </summary>
    //    public const uint NVIDIA_DLSS = 1;
    //}
    //public class FrameRateThreshold
    //{
    //    /// <summary>
    //    /// Indicates the setting is off
    //    /// </summary>
    //    public const uint AlwaysEnabled = 0;

    //    /// <summary>
    //    /// Indicates the setting is on
    //    /// </summary>
    //    public const uint Below30fps = 1;
    //    /// <summary>
    //    /// Indicates the setting is on
    //    /// </summary>
    //    public const uint Below60fps = 2;
    //}
}