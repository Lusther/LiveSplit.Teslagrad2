using System;
using System.Runtime.InteropServices;

namespace LiveSplit.Teslagrad2
{
    // Lumina's world position, X and Y only (the game is 2D)
    public struct PlayerPosition
    {
        public static readonly PlayerPosition Invalid = new PlayerPosition();

        public float X { get; }
        public float Y { get; }
        // False while the player does not exist (menus, loading)
        public bool IsValid { get; }

        public PlayerPosition(float x, float y)
        {
            X = x;
            Y = y;
            IsValid = true;
        }

        // Square of side 2 * radius centered on (centerX, centerY): no square root needed
        public bool IsInSquare(float centerX, float centerY, float radius)
        {
            return IsValid
                && Math.Abs(X - centerX) <= radius
                && Math.Abs(Y - centerY) <= radius;
        }

        public override string ToString() => IsValid ? $"({X:F2}, {Y:F2})" : "(invalid)";
    }

    // Unity's native Vector3, as stored in the TransformHierarchy
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec3
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3 Add(Vec3 o) => new Vec3(X + o.X, Y + o.Y, Z + o.Z);
        public Vec3 Scale(Vec3 o) => new Vec3(X * o.X, Y * o.Y, Z * o.Z);
    }

    // Unity's native Quaternion (x, y, z, w)
    [StructLayout(LayoutKind.Sequential)]
    internal struct Quat
    {
        public float X, Y, Z, W;

        // v + 2w(q x v) + 2 q x (q x v), equivalent to q * v * q^-1 for a unit quaternion
        public Vec3 Rotate(Vec3 v)
        {
            float cx = 2 * (Y * v.Z - Z * v.Y);
            float cy = 2 * (Z * v.X - X * v.Z);
            float cz = 2 * (X * v.Y - Y * v.X);
            return new Vec3(
                v.X + W * cx + (Y * cz - Z * cy),
                v.Y + W * cy + (Z * cx - X * cz),
                v.Z + W * cz + (X * cy - Y * cx));
        }
    }
}
