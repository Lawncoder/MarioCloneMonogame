using MonoGameLibrary.Graphics;

namespace Mario.Components;

public class SpriteTypes(SpriteTypes.SpriteTypesEnum st)
{
    public SpriteTypesEnum SpriteType = st;
    public enum SpriteTypesEnum
    {
        Goomba, Koopa, Mario, Shell, Question
    }
    
}