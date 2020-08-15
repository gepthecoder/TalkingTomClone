using UnityEngine;
using UnityEngine.Events;

public class ClickHandler : MonoBehaviour
{
    public UnityEvent upEvent;
    public UnityEvent downEvent;

    void OnPointerDown()
    {
        downEvent?.Invoke();
    }

    void OnPointerUp()
    {
        upEvent?.Invoke();
    }
}
