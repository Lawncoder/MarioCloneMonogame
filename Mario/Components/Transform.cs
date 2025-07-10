using Microsoft.Xna.Framework;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;


public class Transform
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;

    public Matrix ToMatrix()
    {
        return Matrix.CreateScale(Scale.X, Scale.Y, 1) *
               Matrix.CreateRotationZ(Rotation) *
               Matrix.CreateTranslation(Position.X, Position.Y, 0);
    }

    public void ScaleAroundOrigin(Vector2 scale, Vector2 origin)
    {
        // Adjust position so scaling occurs around the given origin
        Position = origin + (Position - origin) * scale;

        // Apply the new scale
        Scale *= scale;
    }
}