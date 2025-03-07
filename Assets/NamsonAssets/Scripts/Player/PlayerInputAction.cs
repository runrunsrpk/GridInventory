using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputAction : MonoBehaviour
{
    [Header("Mouse Input Values")]
    public Vector2 point;
    public bool leftClick;
    public bool rightClick;
    public bool reset;

    #region Action Messages
    public void OnPoint(InputValue value)
    {
        PointInput(value.Get<Vector2>());
    }

    public void OnLeftClick(InputValue value)
    {
        LeftClickInput(value.isPressed);
    }

    public void OnRightClick(InputValue value)
    {
       RightClickInput(value.isPressed);
    }

    public void OnReset(InputValue value)
    {
        ResetInput(value.isPressed);
    }
    #endregion

    #region Input
    public void PointInput(Vector2 newPoint)
    {
        point = newPoint;
    }

    public void LeftClickInput(bool newClickState)
    {
        //Debug.Log($"On Left Click Input");
        leftClick = newClickState;
    }

    public void RightClickInput(bool newClickState)
    {
        //Debug.Log($"On Right Click Input");
        rightClick = newClickState;
    }

    public void ResetInput(bool newResetState)
    {
        //Debug.Log($"On Reset Input");
        reset = newResetState;
    }
    #endregion
}
