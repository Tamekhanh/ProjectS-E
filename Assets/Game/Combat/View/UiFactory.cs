using UnityEngine;
using UnityEngine.UI;

namespace ProjectS.Combat.View
{
    /// <summary>Các hàm dựng giao diện bằng code, để không phải tạo scene hay prefab bằng tay.</summary>
    public static class UiFactory
    {
        private static Font font;

        /// <summary>Font có sẵn của Unity, hiển thị được tiếng Việt.</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        /// <summary>Chữ phủ kín phần tử cha, có lề.</summary>
        public static Text Label(Transform parent, string name, int size, TextAnchor anchor, float padding = 10f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, padding);

            Text text = go.GetComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button MakeButton(Transform parent, string name, Color color, int textSize, TextAnchor anchor, out Text label)
        {
            RectTransform rect = Panel(parent, name, color);
            Button button = rect.gameObject.AddComponent<Button>();
            label = Label(rect, "Label", textSize, anchor);
            return button;
        }

        /// <summary>Phủ kín phần tử cha, chừa lề <paramref name="padding"/> mỗi cạnh.</summary>
        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Đặt phần tử có kích thước cố định, neo và lấy tâm xoay tại cùng một điểm của phần tử cha.</summary>
        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
