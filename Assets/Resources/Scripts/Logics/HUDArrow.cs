using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDArrow : MonoBehaviour
{
    [HideInInspector]
    public Transform target;

    private Camera m_camera;
    private float halfH, halfW;
    private Image img;
    private Color imgColor;

    // Start is called before the first frame update
    void Start()
    {
        m_camera = Camera.main;
        halfH = Screen.height * 0.5f;
        halfW = Screen.width * 0.5f;
        img = GetComponent<Image>();
        imgColor = img.color;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            img.color = new Color(0f, 0f, 0f, 0f);
            return;
        }

        Vector3 targetScreenPos = m_camera.WorldToScreenPoint(target.position);
        Vector3 targetViewportPos = m_camera.WorldToViewportPoint(target.position);

        if (targetViewportPos.x >= 0 && targetViewportPos.x <= 1 && targetViewportPos.y >= 0 && targetViewportPos.y <= 1)
        {
            img.color = new Color(0f, 0f, 0f, 0f);
            return;
        }

        img.color = imgColor;

        Vector2 onScreenPos = new Vector2(targetViewportPos.x - 0.5f, targetViewportPos.y - 0.5f) * 2;
        float max = Mathf.Max(Mathf.Abs(onScreenPos.x), Mathf.Abs(onScreenPos.y));
        onScreenPos = (onScreenPos / (max * 2));

        onScreenPos.x *= Screen.width;
        onScreenPos.y *= Screen.height;

        onScreenPos.x = Mathf.Clamp(onScreenPos.x, 30f - halfW, halfW - 30f);
        onScreenPos.y = Mathf.Clamp(onScreenPos.y, 30f - halfH, halfH - 30f);

        transform.localPosition = onScreenPos;

        float angle = Mathf.Atan2(targetScreenPos.y, targetScreenPos.x) * Mathf.Rad2Deg;
        angle -= 90f;

        transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }
}
