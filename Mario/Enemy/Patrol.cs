namespace Mario.Enemy;

public struct Patrol
{
    public readonly float DetectionDistance = 0.6f;
    public  readonly float MaxSpeed = 2f;
    public readonly float Acceleration = 100f;
    public bool FacingRight { get; set; }

    public Patrol(float distance, float maxSpeed, float acceleration)
    {
        DetectionDistance = distance;
        MaxSpeed = maxSpeed;
        Acceleration = acceleration;
    }

    public Patrol()
    {
        
    }

}