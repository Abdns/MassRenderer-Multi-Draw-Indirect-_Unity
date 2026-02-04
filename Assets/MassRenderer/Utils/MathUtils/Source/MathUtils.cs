using UnityEngine;

namespace MassRendererSystem.Utils
{
    /// <summary>
    /// Mathematical utility functions for mass rendering calculations.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Generates a random position within a rectangular area centered at the given point.
        /// Randomizes X and Z coordinates while preserving the Y coordinate.
        /// </summary>
        /// <param name="center">Center point of the spawn area.</param>
        /// <param name="areaSize">Size of the area (X = width, Z = depth).</param>
        /// <returns>Random position within the specified area.</returns>
        public static Vector3 GetSpreadPosition(Vector3 center, Vector3 areaSize)
        {
            float halfX = areaSize.x * 0.5f;
            float halfZ = areaSize.z * 0.5f;

            float randomX = Random.Range(-halfX, halfX);
            float randomZ = Random.Range(-halfZ, halfZ);

            return new Vector3(center.x + randomX, center.y, center.z + randomZ);
        }
    }
}
