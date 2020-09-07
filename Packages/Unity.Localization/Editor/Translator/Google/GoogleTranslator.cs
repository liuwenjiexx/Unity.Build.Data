using SimpleJSON;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Localizations
{
    public class GoogleTranslator
    {

        public static void Process(string targetLang, string sourceText, Action<bool, string> callback)
        {
            EditorStartCoroutine(_Process(null, targetLang, sourceText, callback));
        }

        public static void Process(string sourceLang, string targetLang, string sourceText, Action<bool, string> callback)
        {
            EditorStartCoroutine(_Process(sourceLang, targetLang, sourceText, callback));
        }

        static IEnumerator _Process(string sourceLang, string targetLang, string sourceText, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(sourceLang))
                sourceLang = "auto";
            string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
                + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + UnityWebRequest.EscapeURL(sourceText);

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (string.IsNullOrEmpty(webRequest.error))
                {
                    var N = JSONNode.Parse(webRequest.downloadHandler.text);

                    string result = N[0][0][0];
                    callback(true, result);
                }
                else
                {
                    callback(false, webRequest.error);
                }
            }
        }

        static void EditorStartCoroutine(IEnumerator coroutine)
        {
            if (!coroutine.MoveNext())
                return;

            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                if (coroutine.Current != null)
                {
                    var oper = coroutine.Current as AsyncOperation;
                    if (oper != null)
                    {
                        if (!oper.isDone)
                        {
                            EditorApplication.update += callback;
                            return;
                        }
                    }
                    if (coroutine.MoveNext())
                    {
                        EditorApplication.update += callback;
                    }
                }
            };

            EditorApplication.update += callback;
        }
    }
}