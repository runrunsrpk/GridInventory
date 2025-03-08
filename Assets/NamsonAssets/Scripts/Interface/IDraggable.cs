using UnityEngine;

public interface IDraggable
{
    public void OnDrag(Vector2 delta);
    public void OnDrop(Vector2 delta);
}
