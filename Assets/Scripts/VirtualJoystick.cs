using UnityEngine;
using System.Collections;

// Moves control pad in the joystick region
// DecisionMgr will then process conditions based on control pad position
public class VirtualJoystick : MonoBehaviour
{
    [HideInInspector]
    // joystick movement direction used for player movement
    public Vector2 movement = Vector2.zero;

    public Camera uiCamera;

    // PAD ----------------------------------
    public Transform padCenterTransform;
    public float axisClampValue = 0.8f;
    private Vector2 padCenterPos;                   // joystick pivot / start pos
    private Vector2 padCtrlPos;                     // current joystick pad pos
    public float padRadius;         // max distance from center to BG borders

    // PAD LERP ----------------------------------
    private float lerpTime = 1f;
    private float currentLerpTime;
    public float lerpSpeed = 5.0f;
    private float lerpValue = 0;
    private Vector3 startPos;
    private Vector3 endPos;
    private bool isEnabled = true;

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
    }

    public void Awake()
    {
        // activate multitouch
        Input.multiTouchEnabled = true;
        padCtrlPos = Vector2.zero;
        padCenterPos = padCenterTransform.position;
        // LERP ----------------------------------
        startPos = padCenterPos;
        endPos = padCenterPos;
        if (!uiCamera)
            uiCamera = Camera.main;
    }

    public void Update()
    {
        if (!isEnabled) return;

        // MOUSE ----------------------------------------------------------
        // track touches only in game state and in joystick region
        if (Input.GetMouseButton(0))
        {
            // reset lerp timer while mouse is moving
            currentLerpTime = 0f;

            // get touch position 2D
            Vector2 touchPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            // move only if mouse is on the pad
            if (CheckMouseOnPad(touchPosition))
            {
                // move pad controller with touch
                padCtrlPos = touchPosition;
                transform.position = padCtrlPos;

                // joystick axis
                Vector2 direction = (padCtrlPos - padCenterPos);
                movement = direction.normalized;
            }
        }
        // if there is no mouse input - move mouse to center position
        else
        {
            currentLerpTime += Time.deltaTime;
            if (currentLerpTime > lerpTime)
            {
                currentLerpTime = lerpTime;
            }

            // lerp to center pos
            lerpValue = currentLerpTime / lerpTime;
            transform.position = Vector3.Lerp(startPos, endPos, lerpValue * lerpSpeed);

            // no movement
            movement = Vector2.zero;
        }

        // TOUCH ----------------------------------------------------------
        if (Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            // get touch position 2D
            Vector2 touchPosition = new Vector2(touch.position.x, touch.position.y);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // reset lerp timer while mouse is moving
                    currentLerpTime = 0f;
                    // move only if mouse is on the pad
                    if (CheckMouseOnPad(touchPosition))
                    {
                        // move pad controller with touch
                        padCtrlPos = touchPosition;
                        transform.position = padCtrlPos;
                    }
                    break;

                case TouchPhase.Moved:
                    // move only if mouse is on the pad
                    if (CheckMouseOnPad(touchPosition))
                    {
                        // move pad controller with touch
                        padCtrlPos = touchPosition;
                        transform.position = padCtrlPos;

                        // joystick axis
                        Vector2 direction = (padCtrlPos - padCenterPos);
                        movement = direction.normalized;
                    }
                    break;

                case TouchPhase.Canceled:
                    padCtrlPos = padCenterPos;
                    transform.position = padCtrlPos;
                    // no movement
                    movement = Vector2.zero;
                    break;

                case TouchPhase.Ended:
                    // move pad controller with touch
                    if (CheckMouseOnPad(touchPosition))
                    {
                        padCtrlPos = padCenterPos;
                        transform.position = touchPosition;
                    }
                    // no movement
                    movement = Vector2.zero;
                    break;
            }
        }

        //clampAxisValues();
    }

    public void clampAxisValues()
    {
        // make axis output values only in 4 directions
        if (movement.x > axisClampValue || movement.x < -axisClampValue)
        {
            movement.y = 0;
        }
        else if (movement.y > axisClampValue || movement.y < -axisClampValue)
        {
            movement.x = 0;
        }
    }

    public float GetAxis(string AxisName)
    {
        if (AxisName == "Horizontal")
        {
            return movement.x;
        }
        else if (AxisName == "Vertical")
        {
            return movement.y;
        }

        return 0f;
    }

    public bool CheckMouseOnPad(Vector2 mousePos)
    {
        // distance from start pivot to cur pad ctrl position
        float padDistance = Vector2.Distance(mousePos, padCenterPos);
        if (padDistance <= padRadius)
        {
            return true;
        }
        return false;
    }
}
