using nkast.Aether.Physics2D.Common;

namespace Mario.Components;

public class QuestionBlockComponent
{
    public enum QuestionBlockComponentTypes
    {
        Power, Score
    }

    public int Score;
    public QuestionBlockComponentTypes QuestionBlockComponentType;
    


}