using UnityEngine.InputSystem;

// A tap on the screen is what starts the next display.
public static class ScreenAdvance
{
    static bool wasDown;

    public static bool Tapped()
    {
        bool down = Pressed();
        bool tapped = down && !wasDown;
        wasDown = down;
        return tapped;
    }

    static bool Pressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;
        Pointer pointer = Pointer.current;
        return pointer != null && pointer.press.isPressed;
    }
}
