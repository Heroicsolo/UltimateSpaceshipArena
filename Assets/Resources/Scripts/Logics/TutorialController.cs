using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TutorialUnit
{
    public string message;
    public RectTransform target;
}

public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    [SerializeField] Button fullScreenBtn;
    [SerializeField] Transform tutorialMessageTransform;
    [SerializeField] TextMeshProUGUI tutorialMessageLabel;
    [SerializeField] RectTransform highlightFrame;
    [SerializeField] GameObject arrow;
    [SerializeField] float arrowOffset = 30f;

    [SerializeField] List<TutorialUnit> firstTutorial;
    [SerializeField] bool skippableUnits = true;

    private const int referenceScreenW = 1280;
    private const int referenceScreenH = 720;
    private int firstTutorialStep = 0;
    private bool customUnit = false;
    private Action m_endCallback;

    public int TutorialStep => firstTutorialStep;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        tutorialMessageTransform.gameObject.SetActive(false);
        arrow.SetActive(false);
        highlightFrame.gameObject.SetActive(false);
        fullScreenBtn.gameObject.SetActive(false);
    }

    public void ShowFirstTutorial(int step = 0, Action callback = null)
    {
        firstTutorialStep = step;

        customUnit = false;

        m_endCallback = callback;

        ShowTutorialUnit(firstTutorial[firstTutorialStep].message, firstTutorial[firstTutorialStep].target);
    }

    void ShowTutorialUnit(string message, RectTransform target = null)
    {
        tutorialMessageLabel.text = message;
        tutorialMessageTransform.gameObject.SetActive(true);
        tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, -213f, tutorialMessageTransform.localPosition.z);
        fullScreenBtn.gameObject.SetActive(true);

        arrow.SetActive(false);

        if (target != null)
        {
            fullScreenBtn.interactable = false;

            Vector3 targetPos = target.position;

            highlightFrame.position = targetPos;
            highlightFrame.sizeDelta = target.sizeDelta;

            RectTransform arrowRect = arrow.GetComponent<RectTransform>();

            if (targetPos.x < 80f)
            {
                arrowRect.position = targetPos + Vector3.right * (target.rect.width + arrowOffset);
                arrow.transform.localEulerAngles = new Vector3(0f, 0f, 90f);

                if (targetPos.y < Screen.height)
                {
                    tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, -213f, tutorialMessageTransform.localPosition.z);
                }
                else
                {
                    tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, 213f, tutorialMessageTransform.localPosition.z);
                }
            }
            else if (targetPos.x > referenceScreenW - 80f)
            {
                arrowRect.position = targetPos - Vector3.right * (target.rect.width + arrowOffset);
                arrow.transform.localEulerAngles = new Vector3(0f, 0f, -90f);

                if (targetPos.y < Screen.height)
                {
                    tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, -213f, tutorialMessageTransform.localPosition.z);
                }
                else
                {
                    tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, 213f, tutorialMessageTransform.localPosition.z);
                }
            }
            else if (targetPos.y < Screen.height)
            {
                arrowRect.position = targetPos + Vector3.down * (target.rect.height + arrowOffset);
                arrow.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, -213f, tutorialMessageTransform.localPosition.z);
            }
            else
            {
                arrowRect.position = targetPos + Vector3.up * (target.rect.height + arrowOffset);
                arrow.transform.localEulerAngles = new Vector3(0f, 0f, 180f);
                tutorialMessageTransform.localPosition = new Vector3(tutorialMessageTransform.localPosition.x, 213f, tutorialMessageTransform.localPosition.z);
            }

            highlightFrame.gameObject.SetActive(true);
            arrow.GetComponent<FloatingItem>().Restart();
            arrow.SetActive(true);
        }
        else
        {
            highlightFrame.sizeDelta = Vector2.zero;
            highlightFrame.gameObject.SetActive(true);
            fullScreenBtn.interactable = true;
        }

        if (skippableUnits)
            fullScreenBtn.interactable = true;
    }

    public void ShowCustomTutorialUnit(string message, RectTransform target = null, Action callback = null)
    {
        customUnit = true;

        m_endCallback = callback;

        ShowTutorialUnit(message, target);
    }

    public void CloseTutorialUnit()
    {
        if (!customUnit)
        {
            firstTutorialStep++;

            Launcher.instance.SaveTutorialStep();

            if (firstTutorialStep > firstTutorial.Count - 1)
            {
                tutorialMessageTransform.gameObject.SetActive(false);
                arrow.SetActive(false);
                highlightFrame.gameObject.SetActive(false);
                fullScreenBtn.gameObject.SetActive(false);
                Launcher.instance.OnTutorialDone();
                m_endCallback?.Invoke();
            }
            else
            {
                ShowTutorialUnit(firstTutorial[firstTutorialStep].message, firstTutorial[firstTutorialStep].target);
            }
        }
        else
        {
            tutorialMessageTransform.gameObject.SetActive(false);
            arrow.SetActive(false);
            highlightFrame.gameObject.SetActive(false);
            fullScreenBtn.gameObject.SetActive(false);
            m_endCallback?.Invoke();
        }
    }
}
