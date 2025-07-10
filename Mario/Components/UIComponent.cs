namespace Mario.Components;

public struct UIComponent
{
    public Transform Parent;
    public Transform Transform;

    public void ParentToComponent(UIComponent parent)
    {
        Parent = parent.Transform;
        
    }
}