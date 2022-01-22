using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundSwitcher : MonoBehaviour
{
    [SerializeField] private Sprite offIcon;
    [SerializeField] private Sprite onIcon;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    // Start is called before the first frame update
    void Start()
    {
        RefreshSprite();
    }

    void RefreshSprite()
    {
        image.sprite = Launcher.instance.IsSoundOn ? onIcon : offIcon;
    }

    public void SwitchSoundState()
    {
        Launcher.instance.IsSoundOn = !Launcher.instance.IsSoundOn;

        AudioListener.volume = Launcher.instance.IsSoundOn ? 1f : 0f;

        RefreshSprite();
    }
}
