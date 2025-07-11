using System;
using System.Collections;
using Arch.System;
using Arch.Core;
using Arch.Core.Extensions;
using Mario.Components;
using Mario.Helpers;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using World = Arch.Core.World;
using MonoGameLibrary.ECS;

namespace Mario.Systems;

public class DamageSystem : SystemBase
{
    public DamageSystem(World world) : base(world) {}

    public override void Update(in float deltaTime)
    {
        float dTime = deltaTime;
        var query = new QueryDescription().WithAll<DeathComponent, PhysicsComponent>();
        World.Query(in query, (Entity entity, ref DeathComponent component, ref PhysicsComponent physics) =>
        {
            //If player we need to modify game state with events
            if (World.Has<PlatformerCharacter>(entity))
            {
                //Send an event to the game logic which still needs to be implemented
                
                
                    if (component.TimeToActuallyDie >= 0.999f)
                    {
                        
                        physics.Body.LinearVelocity = new Vector2(0, -2f);
                        physics.Fixture.CollidesWith = Category.None;
                        component.TimeToActuallyDie -= dTime;
                        

                    }
                    else
                    {
                        component.TimeToActuallyDie -= dTime;
                        if (component.TimeToActuallyDie <= 0)
                        {
                            Game1.MaxCameraX = 160;
                            if (Game1.PhysicsWorld.BodyList.Contains(physics.Body))
                                Game1.PhysicsWorld.Remove(physics.Body);
                            var marioBody = Game1.PhysicsWorld.CreateBody(new Vector2(2, 10), 0f, BodyType.Dynamic);
                            marioBody.FixedRotation = true;
                            var marioCollider = marioBody.CreateRectangle(1f, 1f, 1, Vector2.Zero);
                            marioCollider.Tag = "Mario";
                            marioCollider.Friction = 0f;
                            marioCollider.Restitution = 0f;
                            marioCollider.CollidesWith = CollisionAssistant.CategoryFromLayers(CollisionLayers.Enemy, CollisionLayers.Ground, CollisionLayers.Block, CollisionLayers.Boundary, CollisionLayers.Wall);
                            marioCollider.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Player);
                            physics = new PhysicsComponent(marioBody, marioCollider);
                            
                            
                            Game1.CommandBuffer.Remove<DeathComponent>(entity);
                            
                            
                            
                           

                            

                           
                         
                        }
                    }
                   

            }
            
            else
            {
               
                //Handle death for enemies
            
                
                    if (component.TimeToActuallyDie >= 0.999f)
                    {
                        
                        physics.Body.LinearVelocity = new Vector2(0, -2f);
                        physics.Fixture.CollidesWith = Category.None;
                        component.TimeToActuallyDie -= dTime;
                        

                    }
                    else
                    {
                        component.TimeToActuallyDie -= dTime;
                        if (component.TimeToActuallyDie <= 0)
                        {
                            if (Game1.PhysicsWorld.BodyList.Contains(physics.Body)) Game1.PhysicsWorld.Remove(physics.Body);
                            Game1.CommandBuffer.Remove<DeathComponent>(entity);

                            if (entity.IsAlive())
                            {
                               //Game1.CommandBuffer.Destroy(entity);
                            }

                           
                         
                        }
                    }
                   

                
            }
        });
        
        query = new QueryDescription().WithAll<HitComponent, PhysicsComponent>();
        World.Query(in query, (Entity entity, ref HitComponent component, ref PhysicsComponent physics) =>
        {
            
            
            if (CollisionAssistant.CategoryInCategories(CollisionLayers.Killer,
                    physics.Fixture.CollisionCategories))
            {
               
                if (CollisionAssistant.CategoryInCategories((int)CollisionLayers.Player + CollisionLayers.Enemy,
                        component.Fixture.CollisionCategories) && component.Entity.IsAlive())
                {
                   
                 
                    if (!World.Has<DeathComponent>(component.Entity))
                    {
                        Game1.CommandBuffer.Add(component.Entity, new DeathComponent());
                    }
                       
                       
                }
            }
            
            
        });
    }

  
}