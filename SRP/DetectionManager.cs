using UnityEngine;
using SRP.ADOFAI.Keyviewer;
using SRP.UI;
using System.Collections.Generic;
using System;

namespace SRP
{
    public class DetectionManager : MonoBehaviour
    {
        public static DetectionManager Instance { get; private set; }

        public DetectionMode Mode = DetectionMode.None;
        public KeyViewerProfile TargetProfile;

        private void Awake() {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI() {
            if (Mode == DetectionMode.None || TargetProfile == null) return;

            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown) {
                KeyCode code = e.keyCode;
                if (code == KeyCode.None || code == KeyCode.Escape) return;

                if (Mode == DetectionMode.Add) {
                    if (!TargetProfile.ActiveKeys.Contains(code)) {
                        TargetProfile.ActiveKeys.Add(code);
                        MainClass.QueueRefresh();
                    }
                } else if (Mode == DetectionMode.Delete) {
                    if (TargetProfile.ActiveKeys.Contains(code)) {
                        TargetProfile.ActiveKeys.Remove(code);
                        MainClass.QueueRefresh();
                    }
                }
            }
        }

        private void Update() {
            MainClass.CheckRefresh();
        }
    }
}
