using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageBox : MonoBehaviour
{
    public static MessageBox instance;

    [SerializeField] private TextMeshProUGUI m_messageField;
    [SerializeField] private GameObject m_messageBox;

    private void Awake()
    {
        if( !instance )
            instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void Show(string message)
    {
        m_messageField.text = message;
        m_messageBox.SetActive(true);
    }
}
