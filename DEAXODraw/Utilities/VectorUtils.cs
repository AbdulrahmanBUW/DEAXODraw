using System;
using Autodesk.Revit.DB;

namespace DEAXODraw.Utilities
{
    public static class VectorUtils
    {
        /// <summary>
        /// Rotates a vector by the specified angle in radians around the Z-axis
        /// </summary>
        /// <param name="vector">Vector to rotate</param>
        /// <param name="angleRadians">Rotation angle in radians</param>
        /// <returns>Rotated vector</returns>
        public static XYZ RotateVector(XYZ vector, double angleRadians)
        {
            try
            {
                double cos = Math.Cos(angleRadians);
                double sin = Math.Sin(angleRadians);

                return new XYZ(
                    vector.X * cos - vector.Y * sin,
                    vector.X * sin + vector.Y * cos,
                    vector.Z
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rotating vector: {ex.Message}");
                return vector; // Return original vector if rotation fails
            }
        }

        /// <summary>
        /// Gets the angle between two vectors in radians
        /// </summary>
        /// <param name="vector1">First vector</param>
        /// <param name="vector2">Second vector</param>
        /// <returns>Angle in radians</returns>
        public static double GetAngleBetweenVectors(XYZ vector1, XYZ vector2)
        {
            try
            {
                vector1 = vector1.Normalize();
                vector2 = vector2.Normalize();

                double dotProduct = vector1.DotProduct(vector2);

                // Clamp to avoid numerical errors
                dotProduct = Math.Max(-1.0, Math.Min(1.0, dotProduct));

                return Math.Acos(dotProduct);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating angle between vectors: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Projects a vector onto the XY plane (sets Z to 0)
        /// </summary>
        /// <param name="vector">Vector to project</param>
        /// <returns>Projected vector</returns>
        public static XYZ ProjectToXY(XYZ vector)
        {
            return new XYZ(vector.X, vector.Y, 0);
        }

        /// <summary>
        /// Checks if two vectors are parallel (within a tolerance)
        /// </summary>
        /// <param name="vector1">First vector</param>
        /// <param name="vector2">Second vector</param>
        /// <param name="tolerance">Tolerance for comparison (default: 1e-6)</param>
        /// <returns>True if vectors are parallel</returns>
        public static bool AreParallel(XYZ vector1, XYZ vector2, double tolerance = 1e-6)
        {
            try
            {
                XYZ cross = vector1.CrossProduct(vector2);
                return cross.GetLength() < tolerance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if vectors are parallel: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a rotation axis line at the specified origin with the given normal
        /// </summary>
        /// <param name="origin">Origin point</param>
        /// <param name="normal">Normal vector</param>
        /// <returns>Line representing the rotation axis</returns>
        public static Line CreateRotationAxis(XYZ origin, XYZ normal)
        {
            try
            {
                XYZ endPoint = origin + normal;
                return Line.CreateBound(origin, endPoint);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating rotation axis: {ex.Message}");
                // Return a default vertical axis if creation fails
                return Line.CreateBound(origin, origin + XYZ.BasisZ);
            }
        }
    }
}