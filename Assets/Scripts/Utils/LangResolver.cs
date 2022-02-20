using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LangResolver : MonoBehaviour
{
    public static LangResolver instance;

    private const char Separator = '=';
    private readonly Dictionary<string, string> _lang = new Dictionary<string, string>();
    private SystemLanguage _language;

    private void Awake()
    {
        if( !instance )
            instance = this;

        DontDestroyOnLoad(gameObject);
        ReadProperties();
    }

    private void ReadProperties()
    {
        _language = Application.systemLanguage;
        var file = Resources.Load<TextAsset>("Localization/" + _language.ToString());
        if (file == null)
        {
            file = Resources.Load<TextAsset>("Localization/" + SystemLanguage.English.ToString());
            _language = SystemLanguage.English;
        }
        foreach (var line in file.text.Split('\n'))
        {
            var prop = line.Split(Separator);
            _lang[prop[0]] = prop[1];
        }
    }

    public string GetLocalizedString(string id)
    {
        return Regex.Unescape(_lang[id]);
    }

    public string GetLocalizedString(string id, int param)
    {
        return Regex.Unescape(_lang[id]).Replace("{0}", param.ToString());
    }

    public string GetLocalizedString(string id, int param, int param2)
    {
        return Regex.Unescape(_lang[id]).Replace("{0}", param.ToString()).Replace("{1}", param2.ToString());
    }

    public string GetLocalizedString(string id, int param, string paramHexColor)
    {
        return Regex.Unescape(_lang[id]).Replace("{0}", "<color=" + paramHexColor + ">" + param.ToString() + "</color>");
    }

    public string GetLocalizedString(string id, string param)
    {
        return Regex.Unescape(_lang[id]).Replace("{0}", param);
    }

    public string GetLocalizedString(string id, string param, string paramHexColor)
    {
        return Regex.Unescape(_lang[id]).Replace("{0}", "<color=" + paramHexColor + ">" + param + "</color>");
    }

    public void ResolveTexts()
    {
        var allTexts = Resources.FindObjectsOfTypeAll<LangText>();
        foreach (var langText in allTexts)
        {
            var text = langText.GetComponent<Text>();
            if( text != null )
            {
                text.text = Regex.Unescape(_lang[langText.Identifier]);
            }
            else
            {
                var textMesh = langText.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = Regex.Unescape(_lang[langText.Identifier]);
                }
                else
                {
                    var textMeshPro = langText.GetComponent<TextMeshProUGUI>();
                    textMeshPro.text = Regex.Unescape(_lang[langText.Identifier]);
                }
            }
        }
    }
}
