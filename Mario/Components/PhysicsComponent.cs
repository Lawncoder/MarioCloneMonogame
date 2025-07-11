using nkast.Aether.Physics2D.Dynamics;

namespace Mario.Components;

public class PhysicsComponent(Body body, Fixture fixture)
{
    public Body Body = body;
    public Fixture Fixture = fixture;
}