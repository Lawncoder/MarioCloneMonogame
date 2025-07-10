using Arch.Core;
using Mario.Helpers;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;

namespace Mario.Components;

public struct HitComponent
{
    public Vector2 CollisionNormal;
    public Category CollisionLayer;
    public FixedArray2<Vector2> Points;
    public Vector2 Position;
    public Entity Entity;
    public Fixture Fixture;
    public Body Body;
}