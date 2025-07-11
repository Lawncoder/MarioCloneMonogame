using System;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Mario.Components;
using Mario.Enemy;
using Mario.Helpers;
using MonoGameLibrary.ECS;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using World = Arch.Core.World;


namespace Mario.Systems;



public class KoopaTroopaShellSystem
    : SystemBase
{
    public KoopaTroopaShellSystem(World world) : base(world)
    {
        _commandBuffer = Game1.CommandBuffer;

    }


    private nkast.Aether.Physics2D.Dynamics.World PhysicsWorld = Game1.PhysicsWorld;
    private CommandBuffer _commandBuffer;
   
    public override void Update(in float deltaTime)
    {
        var query = new  QueryDescription().WithAll<KoopaTroopaComponent,PhysicsComponent, Transform, SpriteTypes>();
        float notIndTime = deltaTime;
        World.Query(in query, (Entity entity, ref KoopaTroopaComponent troopa,  ref PhysicsComponent physics, ref Transform transform, ref SpriteTypes spriteType) =>
       {
           

         
           if (World.Has<HitComponent>(entity))
               
           {
               var hitComponent = World.Get<HitComponent>(entity);
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
                           physics.Fixture.CollisionCategories += (int)CollisionLayers.Killer;
                           physics.Body.LinearVelocity = new nkast.Aether.Physics2D.Common.Vector2(7f * (hitComponent.Position.X - physics.Body.Position.X > 0 ? -1 : 1),
                               physics.Body.LinearVelocity.Y);
                           troopa.Velocity = physics.Body.LinearVelocity.X;
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
               
              
               
           }
            
           if (troopa.IsShelled)
           {
               if (troopa.Moving)
               {
                   physics.Body.LinearVelocity = new Vector2(troopa.Velocity, physics.Body.LinearVelocity.Y);
               }
               else
               {
                   troopa.TimeToBecomeBack -= notIndTime;
                   physics.Body.LinearVelocity = new Vector2(0, physics.Body.LinearVelocity.Y);
                   if (troopa.TimeToBecomeBack <= 0)
                   {
                       troopa.IsShelled = false;
                       Game1.CommandBuffer.Add(entity, new Patrol());
                       spriteType.SpriteType = SpriteTypes.SpriteTypesEnum.Koopa;
                       
                       transform.Scale = new Vector2(16f/196f, 16f/ 290f);
                       

                   }
               }
           }
           
       });
    }
}