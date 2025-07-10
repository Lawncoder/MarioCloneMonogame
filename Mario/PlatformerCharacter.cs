using System;

namespace Mario;

public class PlatformerCharacter
{
    public float MaxSpeed;
    public float Acceleration;
    public float JumpForce;
    public float Gravity;
    public float JumpTime;
    public float JumpHeight;

    public void AutoCalculateGravityAndForce()
    {
        Gravity = JumpHeight/(2*JumpTime * JumpTime);
        JumpForce = (float)Math.Sqrt(2 * JumpHeight * Gravity);
    }

    public void AutoCalculateForce(float gravity, float jumpHeight)
    {
        JumpForce = (float)Math.Sqrt(2 * gravity * jumpHeight);
    }
    
    public States State;

    public enum States
    {
        Grounded,
        Jumping,
        Falling
    }
}