using System;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Mario.Components;
using Mario.Enemy;
using Mario.Helpers;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using World = Arch.Core.World;


namespace Mario.Systems;



public class KoopaTroopaShellSystem
    : BaseSystem<World, float>
{
    public KoopaTroopaShellSystem(World world) : base(world)
    {
        _commandBuffer = Game1.CommandBuffer;

    }


    private nkast.Aether.Physics2D.Dynamics.World PhysicsWorld = Game1.PhysicsWorld;
    private CommandBuffer _commandBuffer;
   
    public override void Update(in float deltaTime)
    {
        var query = new  QueryDescription().WithAll<KoopaTroopaComponent, Body, Transform, SpriteTypes>();
        float notIndTime = deltaTime;
        World.Query(in query, (Entity entity, ref KoopaTroopaComponent troopa, ref HitComponent hitComponent, ref Body body, ref Transform transform, ref SpriteTypes spriteType, ref Fixture fixture) =>
       {
           

         
           if (World.Has<HitComponent>(entity))
           {
               if (troopa.IsShelled)
               {
                   if (troopa.Moving)
                   {
                       

                       if (CollisionAssistant.CategoryInCategories(CollisionLayers.Ground, hitComponent.Fixture.CollisionCategories))
                       {
                          
                           if (hitComponent.CollisionNormal.X * hitComponent.CollisionNormal.X > 0.8)
                           {
                               troopa.Velocity = - troopa.Velocity;
                               
                               
                           }
                       }
                       
                   
                   }
                   if (CollisionAssistant.CategoryInCategories(CollisionLayers.Player, hitComponent.Fixture.CollisionCategories))
                   {
                       if (!troopa.Moving)
                       {
                           fixture.CollisionCategories += (int)CollisionLayers.Killer;
                           body.LinearVelocity = new nkast.Aether.Physics2D.Common.Vector2(7f * (hitComponent.Position.X - body.Position.X > 0 ? -1 : 1),
                               body.LinearVelocity.Y);
                           troopa.Velocity = body.LinearVelocity.X;
                           troopa.Moving = true;
                           
                       }
                   
                   }
              
               }
               else
               { 
                   
                   if (hitComponent.CollisionNormal.Y > 0.8 && CollisionAssistant.CategoryInCategories(CollisionLayers.Player, hitComponent.Fixture.CollisionCategories))
                   {
                       troopa.IsShelled = true;
                       troopa.TimeToBecomeBack = 5f;
                       if (World.Has<Patrol>(entity))
                       {
                           Game1.CommandBuffer.Remove<Patrol>(entity);
                       }

                      
                     
                       
                       spriteType = new SpriteTypes(SpriteTypes.SpriteTypesEnum.Shell);
                       
                       transform.Scale = new Vector2(16f / 577f, 16f/ 482f);
                      
                     
                       
                       


                   }
               
               }
               
               World.Remove<HitComponent>(entity);
               
           }
            
           if (troopa.IsShelled)
           {
               if (troopa.Moving)
               {
                   body.LinearVelocity = new Vector2(troopa.Velocity, body.LinearVelocity.Y);
               }
               else
               {
                   troopa.TimeToBecomeBack -= notIndTime;
                   body.LinearVelocity = new Vector2(0, body.LinearVelocity.Y);
                   if (troopa.TimeToBecomeBack <= 0)
                   {
                       troopa.IsShelled = false;
                       Game1.CommandBuffer.Add(entity, new Patrol());
                       spriteType.SpriteType = SpriteTypes.SpriteTypesEnum.Koopa;
                       
                       transform.Scale = new Vector2(16f/196f, 16f/ 290f);
                       fixture.CollisionCategories -= (int)CollisionLayers.Killer;

                   }
               }
           }
           
       });
    }
}