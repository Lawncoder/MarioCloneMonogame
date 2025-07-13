using System;
using System.Collections.Generic;
using System.Xml.Schema;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Mario.Components;

public class AnimatorControllerComponent
{
    public Dictionary<string, Animation> Animations { get; }
    public string CurrentAnimation;
    public AnimatedSprite Sprite;

    public AnimatorControllerComponent()
    {
        Animations = new();
    }

    public void Update(GameTime gameTime)
    {
        Sprite.Update(gameTime);
    }

    public void ChangeAnimation(string animation)
    {
        var anim = Animations[animation];

        Sprite.Animation = anim;
    }

    public static AnimatorControllerComponent CreateComponentFromFiles(string xml)
    {
        var output = new AnimatorControllerComponent();
        using (var definition = TitleContainer.OpenStream(xml))
        {
            
        }

        return output;
    }
    

}