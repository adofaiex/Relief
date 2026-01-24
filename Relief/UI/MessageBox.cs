using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Relief.UI
{
    public class MessageBox : MonoBehaviour
    {
        private bool showMessageBox = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                showMessageBox = true;
            }
        }

        void OnGUI()
        {
            if (showMessageBox)
            {
                GUI.Box(new Rect(0, 0, Screen.width / 2, Screen.height / 2), "消息框文本");
                if (GUI.Button(new Rect(Screen.width / 4, Screen.height / 4, 100, 50), "关闭"))
                {
                    showMessageBox = false;
                }
            }
        }
    }
}
