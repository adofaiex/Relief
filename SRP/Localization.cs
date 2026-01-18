using System;
using System.Collections.Generic;
using UnityEngine;

namespace SRP
{
    public static class Localization
    {
        private static readonly Dictionary<SystemLanguage, string> FirstTimePopupMessages = new Dictionary<SystemLanguage, string>
        {
            { SystemLanguage.Chinese, "Samui Resource Pack是一个综合性模组包，具有多个Mod的功能，因此在使用时，您需要注重兼容性。" },
            { SystemLanguage.ChineseSimplified, "Samui Resource Pack是一个综合性模组包，具有多个Mod的功能，因此在使用时，您需要注重兼容性。" },
            { SystemLanguage.ChineseTraditional, "Samui Resource Pack是一個綜合性模組包，具有多個Mod的功能，因此在使用時，您需要注重兼容性。" },
            { SystemLanguage.English, "Samui Resource Pack is a comprehensive mod pack that includes features from multiple mods. Therefore, you need to pay attention to compatibility when using it." },
            { SystemLanguage.Korean, "Samui Resource Pack은 여러 모드의 기능을 포함하는 종합 모드 팩입니다. 따라서 사용 시 호환성에 주의해야 합니다." },
            { SystemLanguage.Japanese, "Samui Resource Packは、複数のModの機能を備えた総合的なModパックです。そのため、使用する際は互換性に注意する必要があります。" }
        };

        private static readonly Dictionary<SystemLanguage, string> ButtonTexts = new Dictionary<SystemLanguage, string>
        {
            { SystemLanguage.Chinese, "确定" },
            { SystemLanguage.ChineseSimplified, "确定" },
            { SystemLanguage.ChineseTraditional, "確定" },
            { SystemLanguage.English, "OK" },
            { SystemLanguage.Korean, "확인" },
            { SystemLanguage.Japanese, "確定" }
        };

        public static string GetFirstTimeMessage()
        {
            SystemLanguage lang = Application.systemLanguage;
            // You could also use RDString.language if you want to follow game's setting strictly
            // but Application.systemLanguage is safer if RDString is not yet initialized.
            
            if (FirstTimePopupMessages.TryGetValue(lang, out string msg))
                return msg;
            return FirstTimePopupMessages[SystemLanguage.English];
        }

        public static string GetButtonText()
        {
            SystemLanguage lang = Application.systemLanguage;
            if (ButtonTexts.TryGetValue(lang, out string txt))
                return txt;
            return ButtonTexts[SystemLanguage.English];
        }
    }
}
