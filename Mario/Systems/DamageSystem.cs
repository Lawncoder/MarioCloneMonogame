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

namespace Mario.Systems;

public class DamageSystem : BaseSystem<World,float>
{
    public DamageSystem(World world) : base(world) {}

    public override void Update(in float deltaTime)
    {
        float dTime = deltaTime;
        var query = new QueryDescription().WithAll<DeathComponent>();
        World.Query(in query, (Entity entity, ref DeathComponent component) =>
        {
            //If player we need to modify game state with events
            if (World.Has<PlatformerCharacter>(entity))
            {
                //Send an event to the game logic which still needs to be implemented
                if (World.Has<Body>(entity))
                {
                    if (component.TimeToActuallyDie >= 0.999f)
                    {
                        var body = World.Get<Body>(entity);
                        body.LinearVelocity = new Vector2(0, -2f);
                        foreach (var fixture in body.FixtureList)
                        {
                            fixture.CollidesWith = Category.None;
                        }
                        component.TimeToActuallyDie -= dTime;
                        

                    }
                    else
                    {
                        component.TimeToActuallyDie -= dTime;
                        if (component.TimeToActuallyDie <= 0)
                        {
                            Game1.MaxCameraX = 160;
                            
                            var marioBody = Game1.PhysicsWorld.CreateBody(new Vector2(2, 10), 0f, BodyType.Dynamic);
                            marioBody.FixedRotation = true;
                            var marioCollider = marioBody.CreateRectangle(1f, 1f, 1, Vector2.Zero);
                            marioCollider.Tag = "Mario";
                            marioCollider.Friction = 0f;
                            marioCollider.Restitution = 0f;
                            marioCollider.CollidesWith = CollisionAssistant.CategoryFromLayers(CollisionLayers.Enemy, CollisionLayers.Ground, CollisionLayers.Block, CollisionLayers.Boundary, CollisionLayers.Wall);
                            marioCollider.CollisionCategories = CollisionAssistant.CategoryFromLayers(CollisionLayers.Player);
                            
                           Game1.PhysicsWorld.Remove(World.Get<Body>(entity));
                           if (World.Has<Body>(entity))
                           { World.Remove<Body>(entity);
                            World.Remove<Fixture>(entity);
                               
                           }
                           
                            Game1.CommandBuffer.Add(entity, marioBody);
                            Game1.CommandBuffer.Add(entity, marioCollider);
                            
                            Game1.CommandBuffer.Remove<DeathComponent>(entity);

                            

                           
                         
                        }
                    }
                   

                }
            }
            else
            {
                Console.WriteLine(World.Get<Body>(entity).FixtureList[0].Tag);
                //Handle death for enemies
                if (World.Has<Body>(entity))
                {
                    if (component.TimeToActuallyDie >= 0.999f)
                    {
                        var body = World.Get<Body>(entity);
                        body.LinearVelocity = new Vector2(0, -2f);
                        foreach (var fixture in body.FixtureList)
                        {
                            fixture.CollidesWith = Category.None;
                        }
                        component.TimeToActuallyDie -= dTime;
                        

                    }
                    else
                    {
                        component.TimeToActuallyDie -= dTime;
                        if (component.TimeToActuallyDie <= 0)
                        {
                            Game1.PhysicsWorld.Remove(World.Get<Body>(entity));
                            Game1.CommandBuffer.Remove<DeathComponent>(entity);

                            if (entity.IsAlive())
                            {
                               Game1.CommandBuffer.Destroy(entity);
                            }

                           
                         
                        }
                    }
                   

                }
            }
        });
        
        query = new QueryDescription().WithAll<HitComponent, Body, Fixture>();
        World.Query(in query, (Entity entity, ref HitComponent component, ref Body body, ref Fixture fixture) =>
        {
            
            
            if (CollisionAssistant.CategoryInCategories(CollisionLayers.Killer,
                    fixture.CollisionCategories))
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