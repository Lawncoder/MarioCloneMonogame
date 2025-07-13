using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mario.Components;

public struct UITextComponent(string defaultText, Vector2 position, SpriteFont spriteFont)
{
    public string Text = defaultText;
    public Vector2 Position = position;
    public SpriteFont Font = spriteFont;
}