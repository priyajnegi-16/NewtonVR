using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Unified, robust, and safe Input abstraction for Newton VR Simulation.
/// Seamlessly handles both Unity New Input System and legacy UnityEngine.Input,
/// completely eliminating InvalidOperationException when New Input System is active.
/// </summary>
public static class NewtonInput
{
    /// <summary>
    /// Safe check for key pressed down this frame.
    /// </summary>
    public static bool GetKeyDown(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            switch (key)
            {
                case KeyCode.Space: if (Keyboard.current.spaceKey.wasPressedThisFrame) return true; break;
                case KeyCode.Return: if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) return true; break;
                case KeyCode.Escape: if (Keyboard.current.escapeKey.wasPressedThisFrame) return true; break;
                case KeyCode.Tab: if (Keyboard.current.tabKey.wasPressedThisFrame) return true; break;
                case KeyCode.E: if (Keyboard.current.eKey.wasPressedThisFrame) return true; break;
                case KeyCode.N: if (Keyboard.current.nKey.wasPressedThisFrame) return true; break;
                case KeyCode.V: if (Keyboard.current.vKey.wasPressedThisFrame) return true; break;
                case KeyCode.M: if (Keyboard.current.mKey.wasPressedThisFrame) return true; break;
                case KeyCode.T: if (Keyboard.current.tKey.wasPressedThisFrame) return true; break;
                case KeyCode.K: if (Keyboard.current.kKey.wasPressedThisFrame) return true; break;
                case KeyCode.L: if (Keyboard.current.lKey.wasPressedThisFrame) return true; break;
                case KeyCode.B: if (Keyboard.current.bKey.wasPressedThisFrame) return true; break;
                case KeyCode.P: if (Keyboard.current.pKey.wasPressedThisFrame) return true; break;
                case KeyCode.U: if (Keyboard.current.uKey.wasPressedThisFrame) return true; break;
                case KeyCode.H: if (Keyboard.current.hKey.wasPressedThisFrame) return true; break;
                case KeyCode.O: if (Keyboard.current.oKey.wasPressedThisFrame) return true; break;
                case KeyCode.R: if (Keyboard.current.rKey.wasPressedThisFrame) return true; break;
                case KeyCode.F: if (Keyboard.current.fKey.wasPressedThisFrame) return true; break;
                case KeyCode.C: if (Keyboard.current.cKey.wasPressedThisFrame) return true; break;
                case KeyCode.G: if (Keyboard.current.gKey.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha0: if (Keyboard.current.digit0Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha1: if (Keyboard.current.digit1Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha2: if (Keyboard.current.digit2Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha3: if (Keyboard.current.digit3Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha4: if (Keyboard.current.digit4Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha5: if (Keyboard.current.digit5Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha6: if (Keyboard.current.digit6Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha7: if (Keyboard.current.digit7Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha8: if (Keyboard.current.digit8Key.wasPressedThisFrame) return true; break;
                case KeyCode.Alpha9: if (Keyboard.current.digit9Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad0: if (Keyboard.current.numpad0Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad1: if (Keyboard.current.numpad1Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad2: if (Keyboard.current.numpad2Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad3: if (Keyboard.current.numpad3Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad4: if (Keyboard.current.numpad4Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad5: if (Keyboard.current.numpad5Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad6: if (Keyboard.current.numpad6Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad7: if (Keyboard.current.numpad7Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad8: if (Keyboard.current.numpad8Key.wasPressedThisFrame) return true; break;
                case KeyCode.Keypad9: if (Keyboard.current.numpad9Key.wasPressedThisFrame) return true; break;
                case KeyCode.UpArrow: if (Keyboard.current.upArrowKey.wasPressedThisFrame) return true; break;
                case KeyCode.DownArrow: if (Keyboard.current.downArrowKey.wasPressedThisFrame) return true; break;
                case KeyCode.LeftArrow: if (Keyboard.current.leftArrowKey.wasPressedThisFrame) return true; break;
                case KeyCode.RightArrow: if (Keyboard.current.rightArrowKey.wasPressedThisFrame) return true; break;
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                case KeyCode.Equals: if (Keyboard.current.equalsKey.wasPressedThisFrame || Keyboard.current.numpadPlusKey.wasPressedThisFrame) return true; break;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus: if (Keyboard.current.minusKey.wasPressedThisFrame || Keyboard.current.numpadMinusKey.wasPressedThisFrame) return true; break;
            }
        }
#endif

        try
        {
            return Input.GetKeyDown(key);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safe check for key currently held down.
    /// </summary>
    public static bool GetKey(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            switch (key)
            {
                case KeyCode.Space: if (Keyboard.current.spaceKey.isPressed) return true; break;
                case KeyCode.Return: if (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed) return true; break;
                case KeyCode.Escape: if (Keyboard.current.escapeKey.isPressed) return true; break;
                case KeyCode.Tab: if (Keyboard.current.tabKey.isPressed) return true; break;
                case KeyCode.E: if (Keyboard.current.eKey.isPressed) return true; break;
                case KeyCode.N: if (Keyboard.current.nKey.isPressed) return true; break;
                case KeyCode.V: if (Keyboard.current.vKey.isPressed) return true; break;
                case KeyCode.M: if (Keyboard.current.mKey.isPressed) return true; break;
                case KeyCode.T: if (Keyboard.current.tKey.isPressed) return true; break;
                case KeyCode.K: if (Keyboard.current.kKey.isPressed) return true; break;
                case KeyCode.L: if (Keyboard.current.lKey.isPressed) return true; break;
                case KeyCode.B: if (Keyboard.current.bKey.isPressed) return true; break;
                case KeyCode.P: if (Keyboard.current.pKey.isPressed) return true; break;
                case KeyCode.U: if (Keyboard.current.uKey.isPressed) return true; break;
                case KeyCode.H: if (Keyboard.current.hKey.isPressed) return true; break;
                case KeyCode.O: if (Keyboard.current.oKey.isPressed) return true; break;
                case KeyCode.R: if (Keyboard.current.rKey.isPressed) return true; break;
                case KeyCode.F: if (Keyboard.current.fKey.isPressed) return true; break;
                case KeyCode.C: if (Keyboard.current.cKey.isPressed) return true; break;
                case KeyCode.G: if (Keyboard.current.gKey.isPressed) return true; break;
                case KeyCode.W: if (Keyboard.current.wKey.isPressed) return true; break;
                case KeyCode.A: if (Keyboard.current.aKey.isPressed) return true; break;
                case KeyCode.S: if (Keyboard.current.sKey.isPressed) return true; break;
                case KeyCode.D: if (Keyboard.current.dKey.isPressed) return true; break;
                case KeyCode.UpArrow: if (Keyboard.current.upArrowKey.isPressed) return true; break;
                case KeyCode.DownArrow: if (Keyboard.current.downArrowKey.isPressed) return true; break;
                case KeyCode.LeftArrow: if (Keyboard.current.leftArrowKey.isPressed) return true; break;
                case KeyCode.RightArrow: if (Keyboard.current.rightArrowKey.isPressed) return true; break;
            }
        }
#endif

        try
        {
            return Input.GetKey(key);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safe check for mouse button pressed down this frame (0 = Left, 1 = Right, 2 = Middle).
    /// </summary>
    public static bool GetMouseButtonDown(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (button == 0 && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (button == 1 && Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (button == 2 && Mouse.current.middleButton.wasPressedThisFrame) return true;
        }
#endif

        try
        {
            return Input.GetMouseButtonDown(button);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safe check for mouse button held down.
    /// </summary>
    public static bool GetMouseButton(int button)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (button == 0 && Mouse.current.leftButton.isPressed) return true;
            if (button == 1 && Mouse.current.rightButton.isPressed) return true;
            if (button == 2 && Mouse.current.middleButton.isPressed) return true;
        }
#endif

        try
        {
            return Input.GetMouseButton(button);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safe screen mouse position in pixels.
    /// </summary>
    public static Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }
#endif

        try
        {
            return Input.mousePosition;
        }
        catch
        {
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }
    }

    /// <summary>
    /// Safe mouse delta for looking around on desktop.
    /// </summary>
    public static Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            return delta * 0.1f;
        }
#endif

        try
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            return new Vector2(x, y);
        }
        catch
        {
            return Vector2.zero;
        }
    }

    /// <summary>
    /// Safe horizontal / vertical movement axis vector (-1 to 1).
    /// </summary>
    public static Vector2 GetMoveAxis()
    {
        Vector2 axis = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) axis.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) axis.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) axis.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) axis.x -= 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 padStick = Gamepad.current.leftStick.ReadValue();
            if (padStick.sqrMagnitude > axis.sqrMagnitude) axis = padStick;
        }
#endif

        if (axis == Vector2.zero)
        {
            try
            {
                axis.x = Input.GetAxisRaw("Horizontal");
                axis.y = Input.GetAxisRaw("Vertical");
            }
            catch { }
        }

        if (axis.sqrMagnitude > 1.0f) axis.Normalize();
        return axis;
    }
}

