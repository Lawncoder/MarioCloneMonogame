using System;
using MonoGameLibrary.ECS;
using Arch.Core;
using Mario.Components;
using Mario.Helpers;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Common;

namespace Mario.Systems;

public class PlatformerControllerSystem : SystemBase
{
    public PlatformerControllerSystem(World world) : base(world) {}
    private bool _jumpButtonReleasedSinceLastFalling = true;
    private float _timeSinceJumpButtonPressed = PlatformerCharacter.JUMP_BUFFER;
    private bool jump = false;
    public override void PhysicsUpdate()
    {
        _timeSinceJumpButtonPressed += 20;
        if (Game1.Input.Keyboard.WasKeyJustPressedThisFrame(Keys.Space))
        {
            _timeSinceJumpButtonPressed = 0f;
        }
        
        var marioQuery = new QueryDescription().WithAll<PlatformerCharacter, PhysicsComponent, SpriteTypes, Transform>();
        World.Query(in marioQuery, (Entity entity, ref Transform transform, ref PlatformerCharacter platformerCharacter, ref PhysicsComponent physics, ref SpriteTypes spriteType) =>
        {
            jump = _timeSinceJumpButtonPressed < PlatformerCharacter.JUMP_BUFFER;
            
        
            
            Game1.MaxCameraX = (float)Math.Max(Game1.MaxCameraX, GameDevMath.Physics2ScreenVector(physics.Body.Position).X);
            Game1.Camera.Position.X = GameDevMath.LerpWithClamp(Game1.Camera.Position.X, -Game1.MaxCameraX + Game1.SCREEN_WIDTH * .5f/Game1.Camera.Scale.X, .4f);
            
            var grounded = false;
            Game1.PhysicsWorld.RayCast((fixture, point, normal, fraction) =>
            {
               
                if ((string)fixture.Body.Tag == "Ground")
                {
                    grounded = true;
                    return fraction;
                }

                return 1f;
            }, physics.Body.Position + new Vector2(0.5f, 0), physics.Body.Position + new Vector2(0,0.55f));
            if (!grounded)
            {
                Game1.PhysicsWorld.RayCast((fixture, point, normal, fraction) =>
                {
               
                    if ((string)fixture.Body.Tag == "Ground")
                    {
                        grounded = true;
                        return fraction;
                    }

                    return 1f;
                }, physics.Body.Position - new Vector2(0.5f, 0), physics.Body.Position + new Vector2(0,0.55f));
            }

            var direction = (Game1.Input.Keyboard.IsKeyDown(Keys.Right) ? 1 : 0) -
                            (Game1.Input.Keyboard.IsKeyDown(Keys.Left) ? 1 : 0);
            spriteType.Flip = direction switch
            {
                > 0 => false,
                < 0 => true,
                _ => spriteType.Flip
            };

            
           
            //Applies Gravity. Change this for dashes, floating states etc.
            var velocityY=physics.Body.LinearVelocity.Y;
          
            if (!grounded)
            {
                velocityY += platformerCharacter.Gravity;
              
            }

            var hasDeathComponent = World.Has<DeathComponent>(entity);
            var velocityX = physics.Body.LinearVelocity.X;
            switch (platformerCharacter.State)
            {
                case PlatformerCharacter.States.Falling:
                    if (hasDeathComponent)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Dying;
                    }
                    if (grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Grounded;
                    }
                    if (direction != 0f)
                    {
                        velocityX += direction * platformerCharacter.Acceleration * 0.02f * 0.2f;
                       
                    }
                    else
                    {
                        if (velocityX * velocityX > 0.1f)
                        {
                            velocityX += Math.Sign(-velocityX) * platformerCharacter.Acceleration * 0.02f * 0.2f;
                        }
                        else
                        {
                            velocityX *= 0f;
                        }
                    }

                  
                   
                    velocityX = Math.Clamp(velocityX, -platformerCharacter.MaxSpeed, platformerCharacter.MaxSpeed);
                    physics.Body.LinearVelocity= new Vector2(velocityX, physics.Body.LinearVelocity.Y);
                    if (World.Has<HitComponent>(entity))
                    {
                        HitComponent hitComponent = World.Get<HitComponent>(entity);
                        if (hitComponent.CollisionNormal.Y > 0.8 && CollisionAssistant.CategoryInCategories(CollisionLayers.Enemy, hitComponent.CollisionLayer))
                        {
                            
                            physics.Body.LinearVelocity = new Vector2(physics.Body.LinearVelocity.X, (float)-Math.Sqrt(3*Game1.PhysicsWorld.Gravity.Y));
                            
                        }
                     
                    }
                    
                    break;
                
                case PlatformerCharacter.States.Grounded:
                    if (hasDeathComponent)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Dying;
                    }
                    if (direction != 0f)
                    {
                        velocityX += direction * platformerCharacter.Acceleration * 0.02f;
                       
                    }
                    else
                    {
                        if (velocityX * velocityX > 0.1f)
                        {
                            velocityX += Math.Sign(-velocityX) * platformerCharacter.Acceleration * 0.02f;
                        }
                        else
                        {
                            velocityX *= 0f;
                        }
                    }
                   
                    velocityX = Math.Clamp(velocityX, -platformerCharacter.MaxSpeed, platformerCharacter.MaxSpeed);
                    physics.Body.LinearVelocity= new Vector2(velocityX, physics.Body.LinearVelocity.Y);

                    if (jump && _jumpButtonReleasedSinceLastFalling)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Jumping;
                        physics.Body.LinearVelocity = new  Vector2(physics.Body.LinearVelocity.X, -platformerCharacter.JumpForce);
                        _jumpButtonReleasedSinceLastFalling = false;
                        
                    }
                    
                    if (physics.Body.LinearVelocity.Y > 0.001 && !grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                    }

                   
                    break;

                case PlatformerCharacter.States.Jumping:
                    if (hasDeathComponent)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Dying;
                    }
                    if (physics.Body.LinearVelocity.Y > 0.001 && !grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                    }

                    if (grounded)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Grounded;
                    }

                    if (direction != 0f)
                    {
                        velocityX += direction * platformerCharacter.Acceleration * 0.02f * 0.2f;

                    }
                    else
                    {
                        if (velocityX * velocityX > 0.1f)
                        {
                            velocityX += Math.Sign(-velocityX) * platformerCharacter.Acceleration * 0.02f * 0.2f;
                        }
                        else
                        {
                            velocityX *= 0f;
                        }
                        
                    }

                    if (Game1.Input.Keyboard.IsKeyUp(Keys.Space))
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                        velocityY *= 0.5f;
                        
                    }
                   
                    velocityX = Math.Clamp(velocityX, -platformerCharacter.MaxSpeed, platformerCharacter.MaxSpeed);
                    physics.Body.LinearVelocity= new Vector2(velocityX, velocityY);
                    break;
                case PlatformerCharacter.States.Dying:
                    physics.Body.LinearVelocity = new Vector2(0, physics.Body.LinearVelocity.Y);
                    if (!hasDeathComponent)
                    {
                        platformerCharacter.State = PlatformerCharacter.States.Falling;
                    }
                    break;
                
            }
            
            
            


            if (World.Has<HitComponent>(entity))
            {
                Game1.CommandBuffer.Remove<HitComponent>(entity);
            }
   
        });
        if (Game1.Input.Keyboard.IsKeyUp(Keys.Space))
        {
            _jumpButtonReleasedSinceLastFalling = true;
        }
        //Console.WriteLine($"{jump} was the input valid and {_jumpButtonReleasedSinceLastFalling} have they released the input");
    }
}