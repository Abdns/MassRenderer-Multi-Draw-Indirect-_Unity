using UnityEngine;

namespace VATBakerSystem
{
    /// <summary>
    /// Configuration settings for VAT (Vertex Animation Texture) baking.
    /// Create via Assets > Create > VAT > Baker Settings.
    /// </summary>
    [CreateAssetMenu(menuName = "VAT/Baker Settings")]
    public class VATBakerSettings : ScriptableObject
    {
        /// <summary>
        /// Number of animation samples per second. Higher values = smoother animation but larger textures.
        /// </summary>
        [Min(1f)]
        public float samplesPerSecond = 60f;

        /// <summary>
        /// Texture precision for VAT data. Float is more accurate, Half uses less memory.
        /// </summary>
        public VATPrecision precision = VATPrecision.Float;

        /// <summary>
        /// Texture filtering mode. Bilinear provides smooth interpolation between frames.
        /// </summary>
        public FilterMode filter = FilterMode.Bilinear;

        /// <summary>
        /// Texture wrap mode. Clamp prevents edge artifacts.
        /// </summary>
        public TextureWrapMode wrap = TextureWrapMode.Clamp;

        /// <summary>
        /// Gets the Unity TextureFormat based on the precision setting.
        /// </summary>
        public TextureFormat TextureFormat => precision == VATPrecision.Float
            ? TextureFormat.RGBAFloat
            : TextureFormat.RGBAHalf;
    }

    /// <summary>
    /// Precision level for VAT texture data storage.
    /// </summary>
    public enum VATPrecision : byte
    {
        /// <summary>
        /// 16-bit half precision - smaller memory footprint, less accurate.
        /// </summary>
        Half = 0,

        /// <summary>
        /// 32-bit full precision - larger memory footprint, more accurate.
        /// </summary>
        Float = 1
    }
}
