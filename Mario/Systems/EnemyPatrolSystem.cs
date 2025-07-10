using Arch.Core;
using Arch.System;
using Mario.Enemy;
using Mario.Helpers;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using World = Arch.Core.World;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Mario.Systems;

public class EnemyPatrolSystem : BaseSystem<World, float>
{
    public EnemyPatrolSystem(World world) : base(world) {}

    public override void Update(in float deltaTime)
    {
        var query = new QueryDescription().WithAll<Body, Patrol>();
        World.Query(in query, (Entity entity, ref Body body, ref Patrol goomba) =>
        {
            Vector2 raycastPoint = body.Position + (goomba.FacingRight ? new Vector2(goomba.DetectionDistance, 0) : - new Vector2(goomba.DetectionDistance, 0) );
            bool hit = false;
  
  
            Game1.PhysicsWorld.RayCast(((fixture, point, normal, fraction) =>
                {
                    if (CollisionAssistant.CategoryInCategories(CollisionLayers.Ground + (int)CollisionLayers.Enemy, fixture.CollisionCategories))
                    {
                        hit = true;
                        return fraction;
                    }
                    else
                    {
                        return -1;
                    }
                }),
                body.Position, raycastPoint);

            if (hit)
            {
                goomba.FacingRight = !goomba.FacingRight;
            }
           
            int direction = goomba.FacingRight ? 1 : -1;
           
            float velocityX =  direction *goomba.MaxSpeed;

         
            body.LinearVelocity = new Vector2(velocityX, body.LinearVelocity.Y);
           
           
        });
    }
}