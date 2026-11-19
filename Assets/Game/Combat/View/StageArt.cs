using UnityEngine;

namespace ProjectS.Combat.View
{
    /// <summary>
    /// Các hình vẽ bằng code cho sân thử: sàn đá, phông nền, đốm sáng và hình thế chỗ cho unit chưa có ảnh.
    /// TẠM: thay bằng art thật khi có.
    /// </summary>
    public static class StageArt
    {
        /// <summary>Gạch sàn hầm ngục, lặp lại được. Một ô gạch rộng 1 đơn vị.</summary>
        public static Sprite CreateFloorSprite()
        {
            const int size = 256;
            const int tile = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.07f;
                    float shade = 0.20f + noise;
                    bool isSeam = x % tile < 3 || y % tile < 3;
                    if (isSeam) shade = 0.09f;
                    texture.SetPixel(x, y, new Color(shade * 0.92f, shade * 0.96f, shade * 1.2f, 1f));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), tile, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Phông nền phía xa: tối dần từ chân trời lên trên. Kích thước 1 x 1 đơn vị.</summary>
        public static Sprite CreateBackdropSprite()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var bottom = new Color(0.13f, 0.11f, 0.20f, 1f);
            var top = new Color(0.02f, 0.02f, 0.05f, 1f);

            for (int y = 0; y < size; y++)
            {
                Color row = Color.Lerp(bottom, top, y / (float)(size - 1));
                for (int x = 0; x < size; x++) texture.SetPixel(x, y, row);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Đốm sáng tròn mờ dần ra rìa. Dùng cho ánh sáng kéo thẻ, bóng đổ và vòng dưới chân.</summary>
        public static Sprite CreateGlowSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Hình người bo tròn màu trắng, tô màu theo phe. Kích thước 1 x 2 đơn vị.</summary>
        public static Sprite CreatePlaceholderSprite()
        {
            const int width = 64;
            const int height = 128;
            const float corner = 26f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Khoảng cách tới hình chữ nhật đã co vào một đoạn bằng bán kính bo góc.
                    float px = Mathf.Max(Mathf.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - corner), 0f);
                    float py = Mathf.Max(Mathf.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - corner), 0f);
                    float distance = Mathf.Sqrt(px * px + py * py);
                    float alpha = Mathf.Clamp01(corner - distance);
                    float shade = 0.75f + 0.25f * (y / (float)height);
                    texture.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
        }
    }
}
