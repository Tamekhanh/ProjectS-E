using System.Collections;
using UnityEngine;

namespace ProjectS.Combat.View
{
    /// <summary>
    /// Hiệu ứng trúng đòn dựng bằng Particle System của Unity, cấu hình hoàn toàn bằng code.
    /// Mỗi hiệu ứng là vài GameObject tạm, tự hủy sau khi diễn xong. Chúng được gắn làm con của unit bị đánh,
    /// nên khi trận được dựng lại giữa chừng thì cũng bị hủy theo.
    /// Mọi con số ở đây là chỉnh bằng mắt trên giấy, chưa xem thử: cứ đổi cho tới khi vừa ý.
    /// </summary>
    public static class CombatVfx
    {
        // Vẽ đè lên mọi unit (thân unit cao nhất là 300).
        private const int SortingOrder = 500;

        // Tia sét.
        private const float BoltHeight = 7f;
        private const int BoltSegments = 9;
        private const float BoltJitter = 0.32f;
        private const float BoltWidth = 0.11f;
        private const int BoltFlickers = 3;
        private const float BoltFlickerTime = 0.045f;
        private const float BoltFadeTime = 0.12f;

        private static readonly Color BoltCore = new Color(1f, 1f, 1f, 1f);
        private static readonly Color BoltEdge = new Color(0.55f, 0.8f, 1f, 1f);
        private static readonly Color SparkBright = new Color(1f, 1f, 1f, 1f);
        private static readonly Color SparkBlue = new Color(0.45f, 0.75f, 1f, 1f);

        /// <summary>
        /// Sét đánh từ trên cao xuống một điểm. Tia sét là một đường gấp khúc nháy vài lần, mỗi lần một hình khác.
        /// Gọi ImpactBurst cùng lúc để có chớp sáng và tia lửa tại chỗ trúng.
        /// </summary>
        /// <param name="parent">Unit bị đánh; hiệu ứng là con của nó.</param>
        /// <param name="strikePoint">Điểm sét đánh trúng, thường là ngang ngực mục tiêu.</param>
        /// <param name="lineMaterial">Vật liệu không chịu ánh sáng, không có texture.</param>
        public static IEnumerator LightningBolt(Transform parent, Vector3 strikePoint, Material lineMaterial)
        {
            var go = new GameObject("LightningBolt", typeof(LineRenderer));
            go.transform.SetParent(parent, false);
            LineRenderer line = go.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = BoltSegments + 1;
            line.sortingOrder = SortingOrder;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            if (lineMaterial != null) line.sharedMaterial = lineMaterial;

            // Mảnh ở trên cao, to dần xuống điểm trúng.
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 1f));

            // Nháy vài lần, mỗi lần gấp khúc lại và to nhỏ khác nhau.
            for (int flicker = 0; flicker < BoltFlickers; flicker++)
            {
                if (go == null) yield break;
                SetBoltShape(line, strikePoint);
                line.widthMultiplier = BoltWidth * Random.Range(0.8f, 1.5f);
                SetBoltColor(line, 1f);
                yield return new WaitForSeconds(BoltFlickerTime);
            }

            // Tắt dần.
            for (float time = 0f; time < BoltFadeTime; time += Time.deltaTime)
            {
                if (go == null) yield break;
                SetBoltColor(line, 1f - time / BoltFadeTime);
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        private static void SetBoltShape(LineRenderer line, Vector3 strikePoint)
        {
            Vector3 top = strikePoint + new Vector3(Random.Range(-0.6f, 0.6f), BoltHeight, 0f);
            for (int i = 0; i <= BoltSegments; i++)
            {
                float progress = i / (float)BoltSegments;
                Vector3 point = Vector3.Lerp(top, strikePoint, progress);
                // Hai đầu giữ đúng chỗ, phần giữa lệch ngang ngẫu nhiên thành hình gấp khúc.
                if (i > 0 && i < BoltSegments) point.x += Random.Range(-BoltJitter, BoltJitter);
                line.SetPosition(i, point);
            }
        }

        private static void SetBoltColor(LineRenderer line, float alpha)
        {
            Color start = BoltEdge;
            Color end = BoltCore;
            start.a = alpha;
            end.a = alpha;
            line.startColor = start;
            line.endColor = end;
        }

        /// <summary>
        /// Chỗ sét đánh trúng: một chớp sáng lớn, một chùm tia lửa bắn tung ra rồi rơi xuống, và vài đốm sáng bay lên.
        /// </summary>
        /// <param name="glowMaterial">Vật liệu không chịu ánh sáng, mang texture đốm sáng tròn mờ.</param>
        public static void ImpactBurst(Transform parent, Vector3 strikePoint, Material glowMaterial)
        {
            var root = new GameObject("ImpactBurst");
            root.transform.SetParent(parent, false);
            root.transform.position = strikePoint;

            CreateFlash(root.transform, glowMaterial);
            CreateSparks(root.transform, glowMaterial);
            CreateEmbers(root.transform, glowMaterial);

            Object.Destroy(root, 1.6f);
        }

        /// <summary>Một đốm sáng to bùng lên rồi tắt ngay.</summary>
        private static void CreateFlash(Transform parent, Material material)
        {
            ParticleSystem system = CreateSystem("Flash", parent, material, ParticleSystemRenderMode.Billboard);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.3f;
            main.startLifetime = 0.22f;
            main.startSpeed = 0f;
            main.startSize = 3.2f;
            main.startColor = new Color(0.8f, 0.92f, 1f, 0.9f);
            main.maxParticles = 2;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            FadeOut(system);
            // Bùng to rồi co lại.
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.2f)));

            system.Play();
        }

        /// <summary>Tia lửa bắn tung ra mọi hướng phía trên, kéo dài theo hướng bay, rồi rơi xuống.</summary>
        private static void CreateSparks(Transform parent, Material material)
        {
            ParticleSystem system = CreateSystem("Sparks", parent, material, ParticleSystemRenderMode.Stretch);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(SparkBright, SparkBlue);
            main.gravityModifier = 1.4f;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)30) });

            // Nửa hình cầu mặc định hướng theo trục Z; xoay để nó ngửa lên trên.
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            FadeOut(system);

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.velocityScale = 0.05f;
            renderer.lengthScale = 2.5f;

            system.Play();
        }

        /// <summary>Vài đốm sáng nhỏ bay chậm lên trên sau cú đánh.</summary>
        private static void CreateEmbers(Transform parent, Material material)
        {
            ParticleSystem system = CreateSystem("Embers", parent, material, ParticleSystemRenderMode.Billboard);

            ParticleSystem.MainModule main = system.main;
            main.duration = 0.4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(SparkBlue, SparkBright);
            main.gravityModifier = -0.25f;
            main.maxParticles = 32;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.03f, (short)12) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            FadeOut(system);
            system.Play();
        }

        /// <summary>Tạo một Particle System đã dừng sẵn, chỉ chạy một lần, để cấu hình xong mới cho chạy.</summary>
        private static ParticleSystem CreateSystem(string name, Transform parent, Material material, ParticleSystemRenderMode renderMode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            // Particle System mới tạo tự chạy ngay với thiết lập mặc định. Phải dừng lại thì mới đổi được thời lượng.
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = renderMode;
            renderer.sortingOrder = SortingOrder;
            if (material != null) renderer.sharedMaterial = material;
            return system;
        }

        /// <summary>Hạt mờ dần về trong suốt trong suốt đời sống của nó.</summary>
        private static void FadeOut(ParticleSystem system)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }
    }
}
