using System;
using TMPro;
using UnityEngine;

namespace LibrePad.Utilities
{
    public static class Extensions
    {
        public static bool Active(this VRRig rig) =>
            rig != null && GorillaParent.instance.vrrigs.Contains(rig);

        public static void SafeSetText(this TMP_Text tmp, string text)
        {
            if (tmp == null)
                return;

            if (tmp.text != text)
                tmp.text = text;
        }

        public static void SafeSetFont(this TMP_Text tmp, TMP_FontAsset font)
        {
            if (tmp == null)
                return;

            if (font == null)
                return;

            if (tmp.font.hashCode != font.hashCode)
                tmp.font = font;
        }

        public static void SafeSetFontSize(this TMP_Text tmp, float size)
        {
            if (tmp == null)
                return;

            if (Math.Abs(tmp.fontSize - size) > 0.01f)
                tmp.fontSize = size;
        }

        public static void SafeSetFontStyle(this TMP_Text tmp, FontStyles style)
        {
            if (tmp == null)
                return;

            if (tmp.fontStyle != style)
                tmp.fontStyle = style;
        }

        public static void SafeSetCharacterSpacing(this TMP_Text tmp, float targetSpacing)
        {
            if (tmp == null)
                return;

            if (!Mathf.Approximately(tmp.characterSpacing, targetSpacing))
                tmp.characterSpacing = targetSpacing;
        }

        private static Shader _tmpShader;
        private static Shader TmpShader
        {
            get
            {
                if (_tmpShader == null)
                    _tmpShader = Assets.LoadAsset<Shader>("TMPChams");

                return _tmpShader;
            }
        }

        public static void Chams(this TMP_Text tmp)
        {
            if (tmp == null)
                return;

            var mat = tmp.fontMaterial;
            if (mat != null && mat.shader != TmpShader)
                mat.shader = TmpShader;
        }
    }
}
