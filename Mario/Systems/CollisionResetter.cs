using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Mario.Components;
using MonoGameLibrary.ECS;

namespace Mario.Systems;

public class CollisionResetter: SystemBase
{
    public CollisionResetter(World world) : base(world) {}

    public override void AfterUpdate(in float t)
    {
        var query = new QueryDescription().WithAll<HitComponent>().WithNone<PlatformerCharacter>();
        World.Query(in query, entity =>
        {
           
            Game1.CommandBuffer.Remove<HitComponent>(entity);
            
            
            
        });
    }
}