using System.Collections;
using System.Collections.Generic;
using System.Text;
using ProjectS.Combat.Core;
using ProjectS.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace ProjectS.Combat.View
{
    /// <summary>
    /// Màn combat thử. Sân là không gian 3D nhìn bằng camera phối cảnh: sàn nằm ngang,
    /// 3 nhân vật đứng bên trái và 3 kẻ địch đứng bên phải dưới dạng sprite 2D dựng thẳng.
    /// Giao diện (tay bài, thông tin trên đầu unit) nằm trên một canvas phủ lên sân.
    /// Màn này chỉ hiển thị và nhận thao tác: mọi luật nằm trong BattleState.
    /// Cách chơi: kéo một thẻ khỏi tay bài, thẻ biến thành ánh sáng tại con trỏ, rồi thả vào mục tiêu.
    /// </summary>
    public class CombatScreen : MonoBehaviour
    {
        private const float EnemyTurnDelay = 0.9f;
        private const int MaxLogLines = 4;
        private const int UpcomingTurnsShown = 7;
        private const string PortraitFolder = "GameData/Portraits/";
        private const string AttackSuffix = "_attack";
        private const string HurtSuffix = "_hurt";
        // Ảnh bị đánh: giữ thêm chừng này sau khi hết rung rồi mới về ảnh đứng; chiều cao so với ảnh đứng.
        private const float HurtHold = 0.18f;
        private const float HurtHeightRatio = 0.95f;
        private const string AnimationFolder = "GameData/Animations/";
        // Ảnh minh họa của thẻ mang tên file dữ liệu của thẻ, ví dụ Card_Chem.png.
        private const string CardArtFolder = "GameData/CardArt/";
        // Thẻ trên tay: ảnh minh họa ở trên, chữ ở dưới. Ảnh minh họa có tỉ lệ ngang khoảng 1,57 : 1.
        private const float CardWidth = 205f;
        private const float CardArtHeight = 130f;
        private const float CardTextHeight = 216f;
        private const float HandPadding = 12f;
        /// <summary>Chiều cao phần hình của ảnh ra đòn so với ảnh đứng. Tư thế lao tới thấp hơn đứng thẳng, nhưng có thêm vệt chém.</summary>
        private const float AttackPoseHeightRatio = 0.95f;

        // Bố trí sân.
        private const float FirstUnitX = 1.8f;
        private const float UnitSpacingX = 1.8f;
        private const float FirstUnitZ = 1.0f;
        private const float UnitSpacingZ = 1.0f;
        private const float TallArtHeight = 2.4f;
        private const float ShortArtHeight = 1.6f;
        private const float PlaceholderHeight = 2.1f;

        private const float DragLightSize = 190f;
        private const float DraggedCardAlpha = 0.25f;

        private static readonly Vector3 CameraPosition = new Vector3(0f, 3.2f, -13f);
        private static readonly Vector3 CameraAngles = new Vector3(9f, 0f, 0f);
        private const float CameraFieldOfView = 34f;

        private static readonly Color SkyColor = new Color(0.02f, 0.02f, 0.05f);
        private static readonly Color BarColor = new Color(0.10f, 0.11f, 0.15f, 0.88f);
        private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.10f, 0.82f);
        private static readonly Color PlayerColor = new Color(0.36f, 0.58f, 0.92f);
        private static readonly Color EnemyColor = new Color(0.90f, 0.38f, 0.36f);
        private static readonly Color DeadTint = new Color(0.25f, 0.25f, 0.25f, 0.45f);
        private static readonly Color HpColor = new Color(0.35f, 0.80f, 0.40f);
        private static readonly Color CardColor = new Color(0.30f, 0.30f, 0.38f);
        private static readonly Color CardSelectedColor = new Color(0.70f, 0.56f, 0.16f);
        private static readonly Color CardDisabledColor = new Color(0.20f, 0.20f, 0.24f);
        private static readonly Color ButtonColor = new Color(0.24f, 0.44f, 0.30f);
        private static readonly Color CurrentRingColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        private static readonly Color TargetRingColor = new Color(0.3f, 1f, 0.4f, 0.75f);
        private static readonly Color AttackLightColor = new Color(1f, 0.5f, 0.2f);
        private static readonly Color AllyLightColor = new Color(0.4f, 0.9f, 1f);

        private readonly System.Random rng = new System.Random();
        private readonly List<UnitWidget> widgets = new List<UnitWidget>();
        private readonly Dictionary<GameObject, Unit> unitByObject = new Dictionary<GameObject, Unit>();
        private readonly List<string> logLines = new List<string>();

        private BattleState battle;
        private Camera stageCamera;
        private Transform stageRoot;
        private Sprite glowSprite;
        private Sprite placeholderSprite;
        private Material unlitMaterial;
        // Vật liệu cho hạt của Particle System: cùng shader với sân, mang texture đốm sáng tròn mờ.
        private Material glowParticleMaterial;
        private const string UnlitSpriteShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        private RectTransform canvasRect;
        private RectTransform hitAreasRoot;
        private RectTransform panelsRoot;
        private RectTransform handRoot;
        private Text orderText;
        private Text infoText;
        private Text logText;
        private Text endText;
        private Button endTurnButton;
        private GameObject endOverlay;
        private Image dragLight;
        private CanvasGroup draggedCard;
        // Lớp ánh sáng viền quanh unit được tô sáng.
        private const string GlowShaderPath = "Shaders/SpriteSilhouette";
        private const int GlowCopiesPerRing = 12;
        private const float GlowInnerRadius = 0.045f;
        private const float GlowOuterRadius = 0.10f;
        private const float GlowInnerAlpha = 0.55f;
        private const float GlowOuterAlpha = 0.22f;
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private Material glowMaterial;
        private MaterialPropertyBlock glowProperties;

        private Unit hoveredUnit;
        private bool isOverField;
        private readonly HashSet<Unit> highlighted = new HashSet<Unit>();
        private static readonly Color EnemyHighlightColor = new Color(1f, 0.35f, 0.15f, 0.9f);
        private static readonly Color AllyHighlightColor = new Color(0.3f, 0.85f, 1f, 0.9f);
        private bool isDragging;
        private int selectedIndex = -1;
        private bool dirty;

        // Trình diễn đòn đánh.
        private const float ZoomTime = 0.22f;
        private const float ZoomHold = 0.12f;
        private const float ZoomDistance = 7.5f;
        private const float ClashDistance = 8.5f;
        private const float DashTime = 0.16f;
        private const float StrikeGap = 1.25f;
        private const float LungeDistance = 0.4f;
        private const float LungeTime = 0.06f;
        private const float HitPause = 0.16f;
        private const float HitShakeTime = 0.16f;
        private const float HitShakeAmount = 0.09f;
        private const float FloatTextTime = 0.85f;
        // Kiểu đâm xuyên: thời gian lướt từ trước mặt ra sau lưng mục tiêu.
        private const float PierceTime = 0.14f;
        private const float ClipEndMargin = 0.03f;
        // Đòn sét: khoảng chờ từ lúc tia sét hiện tới lúc trúng đòn, và thời gian giữ sau đó.
        private const float LightningHitDelay = 0.05f;
        private const float LightningHold = 0.22f;
        // Kiểu tầm xa: đạn bay và người bắn giật lùi.
        private const float ProjectileSpeed = 45f;
        private const float ProjectileSize = 0.55f;
        private const float RecoilDistance = 0.18f;
        private static readonly Color ProjectileColor = new Color(1f, 0.8f, 0.35f);
        private static readonly Color HitFlashColor = new Color(1f, 0.35f, 0.35f);

        private readonly Queue<System.Func<IEnumerator>> steps = new Queue<System.Func<IEnumerator>>();
        private RectTransform floatRoot;
        private ActionRecord pendingAction;
        private Unit displayedCurrent;
        private bool busy;
        private int cameraMoveId;
        // Clip ra đòn đang phát và unit đang diễn nó. Mỗi lúc chỉ có một unit ra đòn.
        private PlayableGraph strikeGraph;
        private UnitWidget strikeActor;

        /// <summary>Một hit đã được luật combat tính xong, chờ được diễn trên sân.</summary>
        private class HitRecord
        {
            public Unit Receiver;
            public HitData Hit;
            public int Amount;
            public int HpAfter;
            public int ShieldAfter;
            public bool Died;
        }

        /// <summary>Một lần dùng thẻ hoặc dùng chiêu, kèm các hit của nó.</summary>
        private class ActionRecord
        {
            public Unit Source;
            public Unit Target;
            public string Name;
            public AttackStyle Style;
            public ImpactEffect Effect;
            public readonly List<HitRecord> Hits = new List<HitRecord>();
        }

        private void Awake()
        {
            glowSprite = StageArt.CreateGlowSprite();
            placeholderSprite = StageArt.CreatePlaceholderSprite();
            Shader unlitShader = Shader.Find(UnlitSpriteShader);
            if (unlitShader != null)
            {
                unlitMaterial = new Material(unlitShader);
                glowParticleMaterial = new Material(unlitShader) { mainTexture = glowSprite.texture };
            }

            glowProperties = new MaterialPropertyBlock();
            Shader glowShader = Resources.Load<Shader>(GlowShaderPath);
            if (glowShader != null) glowMaterial = new Material(glowShader);
            else Debug.LogWarning("[ProjectS] Không nạp được shader viền sáng, lớp ánh sáng sẽ hiện bằng màu của ảnh.");

            EnsureEventSystem();
            SetupCamera();
            BuildStage();
            BuildLayout();
            StartBattle();
        }

        private void Update()
        {
            if (!isDragging) return;
            float wave = Mathf.Sin(Time.unscaledTime * 9f);
            float pulse = 1f + 0.12f * wave;
            dragLight.rectTransform.localScale = new Vector3(pulse, pulse, 1f);

            // Lớp ánh sáng viền của các unit đang được tô sáng nhấp nháy theo cùng nhịp.
            foreach (UnitWidget widget in widgets)
            {
                if (widget.Glow.activeSelf) SetGlow(widget, widget.GlowColor, 0.8f + 0.2f * wave);
            }
        }

        private void LateUpdate()
        {
            // Animation chạy xong trước LateUpdate, nên tới đây ảnh và tỉ lệ của frame này đã chốt.
            foreach (UnitWidget widget in widgets)
            {
                // Không có clip nào đang điều khiển thì unit giữ đúng tư thế code đã đặt.
                if (!widget.ClipPlaying)
                {
                    widget.Body.sprite = widget.PoseSprite;
                    widget.Pose.heightRatio = widget.PoseRatio;
                }
                FitBody(widget);
            }
            PositionWidgets();
            if (!dirty) return;
            dirty = false;
            Refresh();
        }

        // ---------- Sân 3D ----------

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            // Module tạo bằng code chưa có bộ action nào, nên phải gán bộ mặc định thì chuột mới bấm được.
            InputSystemUIInputModule module = go.GetComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
            DontDestroyOnLoad(go);
        }

        /// <summary>Chuyển camera của scene sang phối cảnh, nhìn hơi chếch xuống sân.</summary>
        private void SetupCamera()
        {
            stageCamera = Camera.main;
            if (stageCamera == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera));
                go.tag = "MainCamera";
                stageCamera = go.GetComponent<Camera>();
            }

            stageCamera.orthographic = false;
            stageCamera.fieldOfView = CameraFieldOfView;
            stageCamera.nearClipPlane = 0.1f;
            stageCamera.farClipPlane = 100f;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            stageCamera.backgroundColor = SkyColor;
            stageCamera.transform.SetPositionAndRotation(CameraPosition, Quaternion.Euler(CameraAngles));
        }

        private void BuildStage()
        {
            var root = new GameObject("Stage");
            root.transform.SetParent(transform, false);
            stageRoot = root.transform;

            // Phông nền phía xa.
            SpriteRenderer backdrop = CreateStageSprite("Backdrop", StageArt.CreateBackdropSprite(), -300);
            backdrop.transform.position = new Vector3(0f, 10f, 16f);
            backdrop.transform.localScale = new Vector3(80f, 32f, 1f);

            // Sàn nằm ngang, lát gạch lặp lại.
            SpriteRenderer floor = CreateStageSprite("Floor", StageArt.CreateFloorSprite(), -200);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.size = new Vector2(44f, 26f);
            floor.transform.SetPositionAndRotation(new Vector3(0f, 0f, 3f), Quaternion.Euler(90f, 0f, 0f));
        }

        private SpriteRenderer CreateStageSprite(string objectName, Sprite sprite, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(objectName, typeof(SpriteRenderer));
            go.transform.SetParent(parent != null ? parent : stageRoot, false);
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            // Dùng vật liệu không chịu ánh sáng, để sân hiện đúng màu dù scene có đèn 2D hay không.
            if (unlitMaterial != null) renderer.sharedMaterial = unlitMaterial;
            return renderer;
        }

        /// <summary>
        /// Dựng một unit đứng trên sân. Ảnh lấy từ Resources/GameData/Portraits theo tên file dữ liệu,
        /// ví dụ Character_Linh_kiem.png hoặc Enemy_Nhay.png. Chưa có ảnh thì dùng hình thế chỗ tô màu theo phe.
        /// </summary>
        private void CreateWidget(Unit unit, int index, bool leftSide)
        {
            float side = leftSide ? -1f : 1f;
            var position = new Vector3(side * (FirstUnitX + index * UnitSpacingX), 0f, FirstUnitZ - index * UnitSpacingZ);

            string dataName = unit.CharacterData != null ? unit.CharacterData.name : unit.EnemyData.name;
            Sprite art = Resources.Load<Sprite>(PortraitFolder + dataName);
            Sprite sprite = art != null ? art : placeholderSprite;

            // Đo theo phần có hình của ảnh, bỏ qua lề trong suốt, để chân luôn chạm sàn.
            Bounds opaque = OpaqueBounds(sprite);
            float height;
            if (art == null) height = PlaceholderHeight;
            else height = opaque.size.y > opaque.size.x * 1.2f ? TallArtHeight : ShortArtHeight;
            float scale = height / opaque.size.y;
            // Unit ở gần camera hơn phải được vẽ đè lên unit ở xa.
            int order = 100 - Mathf.RoundToInt(position.z * 10f);

            var actor = new GameObject(unit.Name);
            actor.transform.SetParent(stageRoot, false);
            actor.transform.position = position;
            // Animator không cần controller: clip ra đòn được phát thẳng qua Playables khi tới lúc.
            Animator animator = actor.AddComponent<Animator>();
            ActorPose pose = actor.AddComponent<ActorPose>();

            SpriteRenderer shadow = CreateStageSprite("Shadow", glowSprite, -110, actor.transform);
            shadow.color = new Color(0f, 0f, 0f, 0.6f);
            shadow.transform.SetPositionAndRotation(position + new Vector3(0f, 0.02f, 0f), Quaternion.Euler(90f, 0f, 0f));
            shadow.transform.localScale = new Vector3(1.7f, 0.9f, 1f);

            SpriteRenderer ring = CreateStageSprite("Ring", glowSprite, -100, actor.transform);
            ring.transform.SetPositionAndRotation(position + new Vector3(0f, 0.03f, 0f), Quaternion.Euler(90f, 0f, 0f));
            ring.transform.localScale = new Vector3(2.6f, 1.5f, 1f);
            ring.enabled = false;

            SpriteRenderer body = CreateStageSprite("Body", sprite, order, actor.transform);
            body.transform.localScale = new Vector3(scale, scale, 1f);
            // Đặt chân sprite chạm sàn và canh giữa, dù tâm xoay của sprite nằm ở đâu.
            body.transform.localPosition = new Vector3(-opaque.center.x * scale, -opaque.min.y * scale, 0f);

            SpriteRenderer[] glowRenderers = CreateGlow(body, sprite, scale, order - 1, out GameObject glow);

            // Ảnh ra đòn (nếu có) mang tên file dữ liệu kèm đuôi _attack, ví dụ Character_Linh_kiem_attack.png.
            Sprite attackSprite = art != null ? Resources.Load<Sprite>(PortraitFolder + dataName + AttackSuffix) : null;
            Sprite hurtSprite = art != null ? Resources.Load<Sprite>(PortraitFolder + dataName + HurtSuffix) : null;
            Color baseColor = art != null ? Color.white : (leftSide ? PlayerColor : EnemyColor);
            body.color = baseColor;

            // Vùng nhận thả thẻ, phủ lên hình unit trên màn hình.
            var hitObject = new GameObject("Hit " + unit.Name, typeof(RectTransform), typeof(Image));
            hitObject.transform.SetParent(hitAreasRoot, false);
            hitObject.GetComponent<Image>().color = Color.clear;
            var hitRect = (RectTransform)hitObject.transform;
            hitRect.anchorMin = hitRect.anchorMax = hitRect.pivot = new Vector2(0.5f, 0.5f);
            unitByObject[hitObject] = unit;

            // Bảng thông tin nổi trên đầu.
            RectTransform panel = UiFactory.Panel(panelsRoot, "Info " + unit.Name, PanelColor);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.sizeDelta = new Vector2(236f, leftSide ? 92f : 168f);
            unitByObject[panel.gameObject] = unit;

            Text label = UiFactory.Label(panel, "Text", 17, TextAnchor.UpperLeft, 8f);
            label.rectTransform.offsetMin = new Vector2(8f, 14f);

            RectTransform hpBack = UiFactory.Panel(panel, "HpBack", new Color(0f, 0f, 0f, 0.7f));
            hpBack.anchorMin = new Vector2(0f, 0f);
            hpBack.anchorMax = new Vector2(1f, 0f);
            hpBack.pivot = new Vector2(0.5f, 0f);
            hpBack.anchoredPosition = Vector2.zero;
            hpBack.sizeDelta = new Vector2(0f, 10f);
            hpBack.GetComponent<Image>().raycastTarget = false;

            RectTransform hpFill = UiFactory.Panel(hpBack, "HpFill", HpColor);
            UiFactory.Stretch(hpFill);
            hpFill.GetComponent<Image>().raycastTarget = false;

            widgets.Add(new UnitWidget
            {
                Unit = unit,
                ActorRoot = actor,
                Animator = animator,
                Pose = pose,
                DataName = dataName,
                PoseSprite = sprite,
                PoseRatio = 1f,
                Body = body,
                Ring = ring,
                Glow = glow,
                GlowRenderers = glowRenderers,
                BaseColor = baseColor,
                HomePosition = position,
                BodyRestPosition = body.transform.localPosition,
                BodyOrder = order,
                IdleSprite = sprite,
                AttackSprite = attackSprite,
                HurtSprite = hurtSprite,
                DisplayHp = unit.Hp,
                DisplayShield = unit.Shield,
                Height = height,
                Width = opaque.size.x * scale,
                Panel = panel,
                Text = label,
                HpFill = hpFill,
                HitArea = hitRect
            });
        }

        /// <summary>
        /// Tạo lớp ánh sáng ôm sát viền sprite: hai vòng bản sao hình bóng của chính sprite đó,
        /// đặt lệch ra mọi hướng phía sau thân. Phần ló ra ngoài thân tạo thành viền sáng đúng theo dáng unit.
        /// </summary>
        private SpriteRenderer[] CreateGlow(SpriteRenderer body, Sprite sprite, float bodyScale, int sortingOrder, out GameObject glow)
        {
            glow = new GameObject("Glow");
            glow.transform.SetParent(body.transform, false);

            var renderers = new SpriteRenderer[GlowCopiesPerRing * 2];
            for (int i = 0; i < renderers.Length; i++)
            {
                bool outerRing = i >= GlowCopiesPerRing;
                float angle = (i % GlowCopiesPerRing) * Mathf.PI * 2f / GlowCopiesPerRing;
                float radius = outerRing ? GlowOuterRadius : GlowInnerRadius;

                SpriteRenderer copy = CreateStageSprite("GlowCopy", sprite, sortingOrder, glow.transform);
                if (glowMaterial != null) copy.sharedMaterial = glowMaterial;
                // Lớp sáng là con của thân nên chịu chung tỉ lệ của thân: chia lại để độ lệch đúng theo đơn vị sân.
                copy.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (radius / bodyScale)
                    + new Vector3(0f, 0f, 0.01f / bodyScale);
                renderers[i] = copy;
            }

            glow.SetActive(false);
            return renderers;
        }

        /// <summary>Đặt màu và độ sáng cho lớp ánh sáng viền. Vòng ngoài mờ hơn vòng trong để viền tan dần ra.</summary>
        private void SetGlow(UnitWidget widget, Color color, float intensity)
        {
            for (int i = 0; i < widget.GlowRenderers.Length; i++)
            {
                bool outerRing = i >= GlowCopiesPerRing;
                Color copyColor = color;
                copyColor.a = (outerRing ? GlowOuterAlpha : GlowInnerAlpha) * intensity;

                SpriteRenderer copy = widget.GlowRenderers[i];
                copy.color = copyColor;
                copy.GetPropertyBlock(glowProperties);
                glowProperties.SetColor(GlowColorId, copyColor);
                copy.SetPropertyBlock(glowProperties);
            }
        }

        /// <summary>Vùng có hình của sprite, tính từ lưới bao sát của nó. Lề trong suốt quanh ảnh không được tính.</summary>
        private static Bounds OpaqueBounds(Sprite sprite)
        {
            Vector2[] vertices = sprite.vertices;
            if (vertices == null || vertices.Length == 0) return sprite.bounds;

            Vector2 min = vertices[0];
            Vector2 max = vertices[0];
            foreach (Vector2 vertex in vertices)
            {
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
            }
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        /// <summary>Đổi unit sang ảnh ra đòn hoặc về ảnh đứng. Unit không có ảnh ra đòn thì giữ nguyên.</summary>
        private static void SetAttackPose(UnitWidget widget, bool attacking)
        {
            if (widget.AttackSprite == null) return;
            if (attacking) SetPose(widget, widget.AttackSprite, AttackPoseHeightRatio);
            else SetPose(widget, widget.IdleSprite, 1f);
        }

        /// <summary>Đặt tư thế cho unit: hiện ảnh nào, cao bao nhiêu so với ảnh đứng. Giữ tới khi được đặt lại hoặc có clip phát.</summary>
        private static void SetPose(UnitWidget widget, Sprite sprite, float heightRatio)
        {
            widget.PoseSprite = sprite;
            widget.PoseRatio = heightRatio;
            if (widget.ClipPlaying) return;
            widget.Body.sprite = sprite;
            widget.Pose.heightRatio = heightRatio;
            FitBody(widget);
        }

        /// <summary>
        /// Canh ảnh đang hiện của unit: phóng cho đúng chiều cao, đặt chân chạm sàn và canh giữa chỗ đứng.
        /// Ảnh có thể do code đặt hoặc do clip animation đổi, nên hàm này chỉ nhìn kết quả cuối và canh lại khi nó đổi.
        /// </summary>
        private static void FitBody(UnitWidget widget)
        {
            Sprite sprite = widget.Body.sprite;
            if (sprite == null) return;
            float ratio = Mathf.Max(0.1f, widget.Pose.heightRatio);
            bool flipped = widget.Body.flipX;
            if (sprite == widget.FittedSprite && flipped == widget.FittedFlip && Mathf.Approximately(ratio, widget.FittedRatio)) return;

            // Đo theo phần có hình của ảnh, bỏ qua lề trong suốt.
            Bounds opaque = OpaqueBounds(sprite);
            float scale = widget.Height * ratio / opaque.size.y;
            var rest = new Vector3(-opaque.center.x * scale, -opaque.min.y * scale, 0f);
            // Lật ảnh quanh tâm xoay làm phần có hình lệch sang bên kia, nên độ lệch ngang phải đổi dấu theo.
            if (flipped) rest.x = -rest.x;

            widget.BodyRestPosition = rest;
            widget.Body.transform.localScale = new Vector3(scale, scale, 1f);
            widget.Body.transform.localPosition = rest;
            widget.FittedSprite = sprite;
            widget.FittedRatio = ratio;
            widget.FittedFlip = flipped;
        }

        /// <summary>
        /// Clip cho một nhát đánh, hoặc null nếu unit chưa có clip nào hợp.
        /// Đòn tầm xa ưu tiên clip "_ranged" (tụ phép, bắn). Đòn cận chiến ưu tiên clip theo thuộc tính của hit
        /// ("_slash", "_pierce", "_blunt"...), để một thẻ slash, pierce, slash đổi động tác theo từng nhát.
        /// Không có thì thử clip theo kiểu ra đòn ("_lunge", "_inplace"...), cách đặt tên cũ.
        /// Ví dụ Character_Linh_kiem_pierce, Character_Phap_su_ranged.
        /// </summary>
        private static AnimationClip FindStrikeClip(UnitWidget widget, AttackStyle style, HitAttribute attribute)
        {
            string styleKey = style.ToString().ToLowerInvariant();
            string attributeKey = attribute.ToString().ToLowerInvariant();
            string first = style == AttackStyle.Ranged ? styleKey : attributeKey;
            string second = style == AttackStyle.Ranged ? attributeKey : styleKey;
            AnimationClip clip = Resources.Load<AnimationClip>(AnimationFolder + widget.DataName + "_" + first);
            if (clip == null) clip = Resources.Load<AnimationClip>(AnimationFolder + widget.DataName + "_" + second);
            return clip;
        }

        /// <summary>Thời điểm đòn đánh trúng trong clip: vị trí của sự kiện Strike. Clip không có sự kiện đó thì lấy giữa clip.</summary>
        private static float StrikeTimeOf(AnimationClip clip)
        {
            foreach (AnimationEvent animationEvent in clip.events)
            {
                if (animationEvent.functionName == ActorPose.StrikeEvent) return Mathf.Clamp(animationEvent.time, 0f, clip.length);
            }
            return clip.length * 0.5f;
        }

        private void PlayStrikeClip(UnitWidget widget, AnimationClip clip)
        {
            StopStrikeClip();
            AnimationPlayableUtilities.PlayClip(widget.Animator, clip, out strikeGraph);
            widget.ClipPlaying = true;
            strikeActor = widget;
        }

        /// <summary>Dừng clip ra đòn. Unit giữ nguyên tư thế cuối của clip cho tới khi code đặt tư thế khác.</summary>
        private void StopStrikeClip()
        {
            if (strikeActor != null)
            {
                strikeActor.PoseSprite = strikeActor.Body.sprite;
                strikeActor.PoseRatio = strikeActor.Pose.heightRatio;
                strikeActor.ClipPlaying = false;
                strikeActor = null;
            }
            if (strikeGraph.IsValid()) strikeGraph.Destroy();
        }

        private void OnDestroy()
        {
            strikeActor = null;
            if (strikeGraph.IsValid()) strikeGraph.Destroy();
        }

        /// <summary>Mỗi frame, đặt bảng thông tin và vùng nhận thả thẻ theo vị trí của unit trên màn hình.</summary>
        private void PositionWidgets()
        {
            if (stageCamera == null || canvasRect == null) return;

            foreach (UnitWidget widget in widgets)
            {
                Vector3 feet = widget.ActorRoot.transform.position;
                Vector3 head = feet + new Vector3(0f, widget.Height + 0.12f, 0f);
                widget.Panel.anchoredPosition = WorldToCanvas(head);

                float halfWidth = widget.Width * 0.5f;
                Vector2 bottomLeft = WorldToCanvas(feet + new Vector3(-halfWidth, 0f, 0f));
                Vector2 topRight = WorldToCanvas(feet + new Vector3(halfWidth, widget.Height, 0f));
                widget.HitArea.anchoredPosition = (bottomLeft + topRight) * 0.5f;
                widget.HitArea.sizeDelta = new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
            }
        }

        private Vector2 WorldToCanvas(Vector3 worldPoint)
        {
            Vector3 screen = stageCamera.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            return local;
        }

        // ---------- Giao diện ----------

        private void BuildLayout()
        {
            var canvasObject = new GameObject("CombatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRect = (RectTransform)canvasObject.transform;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasObject.transform;

            // Hai lớp đi theo unit trên sân: vùng nhận thả thẻ ở dưới, bảng thông tin ở trên.
            hitAreasRoot = CreateLayer(root, "HitAreas");
            panelsRoot = CreateLayer(root, "UnitPanels");
            floatRoot = CreateLayer(root, "FloatingText");

            // Thanh thứ tự turn ở trên cùng.
            RectTransform orderBar = UiFactory.Panel(root, "TurnOrder", BarColor);
            orderBar.anchorMin = new Vector2(0f, 1f);
            orderBar.anchorMax = new Vector2(1f, 1f);
            orderBar.pivot = new Vector2(0.5f, 1f);
            orderBar.anchoredPosition = Vector2.zero;
            orderBar.sizeDelta = new Vector2(0f, 64f);
            orderText = UiFactory.Label(orderBar, "Text", 24, TextAnchor.MiddleCenter);

            // Nhật ký trận: vài dòng gần nhất, ngay dưới thanh thứ tự.
            RectTransform logPanel = UiFactory.Panel(root, "Log", PanelColor);
            UiFactory.Place(logPanel, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(880f, 112f));
            logPanel.GetComponent<Image>().raycastTarget = false;
            logText = UiFactory.Label(logPanel, "Text", 20, TextAnchor.LowerLeft, 10f);

            // Tay bài ở dưới giữa.
            handRoot = UiFactory.Panel(root, "Hand", BarColor);
            UiFactory.Place(handRoot, new Vector2(0.5f, 0f), new Vector2(0f, 20f),
                new Vector2(1120f, CardArtHeight + CardTextHeight + HandPadding * 2f));
            HorizontalLayoutGroup layout = handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Thông tin AP và deck ở dưới trái.
            RectTransform infoPanel = UiFactory.Panel(root, "Info", BarColor);
            UiFactory.Place(infoPanel, new Vector2(0f, 0f), new Vector2(40f, 20f), new Vector2(330f, 280f));
            infoText = UiFactory.Label(infoPanel, "Text", 26, TextAnchor.UpperLeft, 16f);

            // Nút kết thúc turn ở dưới phải.
            endTurnButton = UiFactory.MakeButton(root, "EndTurn", ButtonColor, 30, TextAnchor.MiddleCenter, out Text endTurnLabel);
            UiFactory.Place((RectTransform)endTurnButton.transform, new Vector2(1f, 0f), new Vector2(-40f, 20f), new Vector2(330f, 100f));
            endTurnLabel.text = "Kết thúc turn";
            endTurnButton.onClick.AddListener(OnEndTurnClicked);

            // Ánh sáng đi theo con trỏ khi kéo thẻ.
            var lightObject = new GameObject("DragLight", typeof(RectTransform), typeof(Image));
            lightObject.transform.SetParent(root, false);
            dragLight = lightObject.GetComponent<Image>();
            dragLight.sprite = glowSprite;
            dragLight.raycastTarget = false;
            dragLight.rectTransform.sizeDelta = new Vector2(DragLightSize, DragLightSize);
            lightObject.SetActive(false);

            // Lớp phủ khi trận kết thúc.
            RectTransform overlay = UiFactory.Panel(root, "EndOverlay", new Color(0f, 0f, 0f, 0.8f));
            UiFactory.Stretch(overlay);
            endOverlay = overlay.gameObject;
            endText = UiFactory.Label(overlay, "Text", 96, TextAnchor.MiddleCenter);
            Button restart = UiFactory.MakeButton(overlay, "Restart", ButtonColor, 34, TextAnchor.MiddleCenter, out Text restartLabel);
            UiFactory.Place((RectTransform)restart.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(320f, 100f));
            restartLabel.text = "Chơi lại";
            restart.onClick.AddListener(StartBattle);
            endOverlay.SetActive(false);
        }

        private static RectTransform CreateLayer(Transform parent, string layerName)
        {
            var go = new GameObject(layerName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            UiFactory.Stretch(rect);
            return rect;
        }

        private void StartBattle()
        {
            StopAllCoroutines();
            strikeActor = null;
            if (strikeGraph.IsValid()) strikeGraph.Destroy();
            foreach (UnitWidget widget in widgets)
            {
                Destroy(widget.ActorRoot);
                Destroy(widget.Panel.gameObject);
                Destroy(widget.HitArea.gameObject);
            }
            widgets.Clear();
            unitByObject.Clear();
            logLines.Clear();
            steps.Clear();
            busy = false;
            pendingAction = null;
            displayedCurrent = null;
            selectedIndex = -1;
            StopDragVisuals();
            endOverlay.SetActive(false);
            for (int i = floatRoot.childCount - 1; i >= 0; i--) Destroy(floatRoot.GetChild(i).gameObject);
            stageCamera.transform.position = CameraPosition;

            battle = BattleSetup.Create(rng);
            if (battle == null)
            {
                logText.text = "Thiếu dữ liệu mẫu. Chạy menu ProjectS > Tạo lại dữ liệu mẫu rồi bấm Play lại.";
                return;
            }

            battle.TurnStarted += OnTurnStarted;
            battle.CardPlayed += OnCardPlayed;
            battle.EnemyMoveUsed += OnEnemyMoveUsed;
            battle.HitResolved += OnHitResolved;
            battle.UnitDied += OnUnitDied;
            battle.BattleEnded += OnBattleEnded;

            for (int i = 0; i < battle.Players.Count; i++) CreateWidget(battle.Players[i], i, true);
            for (int i = 0; i < battle.Enemies.Count; i++) CreateWidget(battle.Enemies[i], i, false);

            battle.Start();
            dirty = true;
        }

        // ---------- Thao tác của người chơi ----------

        private bool IsPlayerTurn => battle != null
            && battle.Outcome == BattleOutcome.Ongoing
            && battle.Current != null
            && battle.Current.IsPlayer;

        /// <summary>Người chơi chỉ thao tác được khi tới turn của mình và sân đã diễn xong mọi thứ.</summary>
        private bool CanAct => IsPlayerTurn && !busy && displayedCurrent == battle.Current;

        private CardData SelectedCard
        {
            get
            {
                if (!IsPlayerTurn) return null;
                IReadOnlyList<CardData> hand = battle.Current.Deck.Hand;
                return selectedIndex >= 0 && selectedIndex < hand.Count ? hand[selectedIndex] : null;
            }
        }

        /// <summary>Bắt đầu kéo một thẻ: thẻ mờ đi và ánh sáng hiện tại con trỏ.</summary>
        public void BeginCardDrag(CardDragHandler handler, PointerEventData eventData)
        {
            if (!CanAct || isDragging) return;
            IReadOnlyList<CardData> hand = battle.Current.Deck.Hand;
            int index = handler.HandIndex;
            if (index < 0 || index >= hand.Count) return;

            CardData card = hand[index];
            if (card.apCost > battle.Current.Ap)
            {
                AddLog("Không đủ AP cho " + card.displayName + ".");
                return;
            }

            isDragging = true;
            selectedIndex = index;
            hoveredUnit = null;

            draggedCard = handler.GetComponent<CanvasGroup>();
            if (draggedCard != null) draggedCard.alpha = DraggedCardAlpha;

            dragLight.color = card.TargetsAlly ? AllyLightColor : AttackLightColor;
            dragLight.gameObject.SetActive(true);
            dragLight.rectTransform.position = eventData.position;

            RefreshWidgets();
        }

        public void UpdateCardDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            dragLight.rectTransform.position = eventData.position;

            Unit hovered = ValidTargetUnder(eventData);
            bool overField = !IsOverHand(eventData);
            if (hovered == hoveredUnit && overField == isOverField) return;
            hoveredUnit = hovered;
            isOverField = overField;

            // Tô sáng mọi unit sẽ chịu tác động nếu thả thẻ ngay lúc này.
            // Thẻ có mục tiêu chính: tính theo unit đang nằm dưới con trỏ.
            // Thẻ không cần mục tiêu (ví dụ đánh mọi địch): tính ngay khi thẻ được kéo ra khỏi tay bài.
            highlighted.Clear();
            CardData card = SelectedCard;
            if (card != null)
            {
                bool willPlay = card.NeedsTarget ? hovered != null : overField;
                if (willPlay) highlighted.UnionWith(battle.PreviewReceivers(card, hovered));
            }
            RefreshWidgets();
        }

        /// <summary>Thả thẻ: trúng mục tiêu hợp lệ thì đánh, không thì thẻ quay về tay.</summary>
        public void EndCardDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            CardData card = SelectedCard;
            Unit target = ValidTargetUnder(eventData);
            Unit anyUnit = UnitUnder(eventData);
            StopDragVisuals();

            if (card != null)
            {
                if (!card.NeedsTarget)
                {
                    // Thẻ không cần mục tiêu: thả ở bất kỳ đâu ngoài tay bài là đánh.
                    if (!IsOverHand(eventData)) battle.PlayCard(card, null);
                }
                else if (target != null)
                {
                    battle.PlayCard(card, target);
                }
                else if (anyUnit != null && anyUnit.IsAlive)
                {
                    AddLog(card.TargetsAlly ? "Thẻ này phải thả vào đồng đội." : "Thẻ này phải thả vào kẻ địch.");
                }
            }

            selectedIndex = -1;
            dirty = true;
        }

        private void StopDragVisuals()
        {
            isDragging = false;
            hoveredUnit = null;
            isOverField = false;
            highlighted.Clear();
            if (draggedCard != null) draggedCard.alpha = 1f;
            draggedCard = null;
            if (dragLight != null) dragLight.gameObject.SetActive(false);
        }

        private Unit UnitUnder(PointerEventData eventData)
        {
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit == null) return null;
            return unitByObject.TryGetValue(hit, out Unit unit) ? unit : null;
        }

        private Unit ValidTargetUnder(PointerEventData eventData)
        {
            CardData card = SelectedCard;
            Unit unit = UnitUnder(eventData);
            return card != null && unit != null && battle.IsValidTarget(card, unit) ? unit : null;
        }

        private bool IsOverHand(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(handRoot, eventData.position, null);
        }

        private void OnEndTurnClicked()
        {
            if (!CanAct) return;
            selectedIndex = -1;
            battle.EndTurn();
            dirty = true;
        }

        // ---------- Sự kiện từ luật combat ----------

        // Luật combat tính xong mọi thứ ngay lập tức. Các hàm dưới đây chỉ ghi lại chuyện đã xảy ra
        // vào một hàng đợi, rồi sân diễn lại lần lượt từng bước cho người chơi xem.

        private void OnTurnStarted(Unit unit)
        {
            pendingAction = null;
            Enqueue(() => ShowTurnStart(unit));
        }

        private void OnCardPlayed(Unit source, CardData card, Unit target)
        {
            // Mặc định thẻ diễn theo kiểu của nhân vật đang dùng nó, vì thẻ nào cũng gắn được cho nhân vật nào.
            // Thẻ đòi một kiểu nhất định (ví dụ phép tầm gần) thì kiểu của thẻ thắng.
            AttackStyle characterStyle = source.CharacterData != null ? source.CharacterData.attackStyle : AttackStyle.Lunge;
            BeginAction(source, target, card.displayName, card.ResolveAttackStyle(characterStyle), card.impactEffect);
        }

        private void OnEnemyMoveUsed(Unit source, EnemyMove move, Unit target)
        {
            BeginAction(source, target, move.displayName, move.attackStyle, ImpactEffect.Projectile);
        }

        private void BeginAction(Unit source, Unit target, string actionName, AttackStyle style, ImpactEffect effect)
        {
            var action = new ActionRecord { Source = source, Target = target, Name = actionName, Style = style, Effect = effect };
            pendingAction = action;
            Enqueue(() => AnimateAction(action));
        }

        private void OnHitResolved(Unit source, Unit receiver, HitData hit, int amount)
        {
            if (pendingAction == null) return;
            pendingAction.Hits.Add(new HitRecord
            {
                Receiver = receiver,
                Hit = hit,
                Amount = amount,
                HpAfter = receiver.Hp,
                ShieldAfter = receiver.Shield
            });
        }

        private void OnUnitDied(Unit unit)
        {
            if (pendingAction == null || pendingAction.Hits.Count == 0) return;
            pendingAction.Hits[pendingAction.Hits.Count - 1].Died = true;
        }

        private void OnBattleEnded(BattleOutcome outcome)
        {
            Enqueue(() => ShowBattleEnd(outcome));
        }

        // ---------- Trình diễn trên sân ----------

        private void Enqueue(System.Func<IEnumerator> step)
        {
            steps.Enqueue(step);
            if (busy) return;
            busy = true;
            StartCoroutine(RunSteps());
        }

        private IEnumerator RunSteps()
        {
            while (true)
            {
                // Chờ một frame để mọi sự kiện phát ra trong cùng một lệnh kịp được ghi lại trước khi diễn.
                yield return null;
                if (steps.Count == 0) break;
                yield return steps.Dequeue()();
            }
            busy = false;
            dirty = true;
        }

        private IEnumerator ShowTurnStart(Unit unit)
        {
            displayedCurrent = unit;
            selectedIndex = -1;

            // Shield của unit mất ở đầu turn của chính nó.
            UnitWidget widget = FindWidget(unit);
            if (widget != null) widget.DisplayShield = 0;

            AddLog("<b>— Turn của " + unit.Name + " —</b>");
            Refresh();

            if (unit.IsPlayer) yield break;
            yield return new WaitForSeconds(EnemyTurnDelay);
            battle.ExecuteEnemyTurn();
        }

        private IEnumerator ShowBattleEnd(BattleOutcome outcome)
        {
            yield return new WaitForSeconds(0.5f);
            endText.text = outcome == BattleOutcome.Victory ? "THẮNG" : "THUA";
            endOverlay.SetActive(true);
        }

        /// <summary>
        /// Diễn một lần dùng thẻ hoặc chiêu: camera zoom vào người ra đòn, người đó ra đòn theo kiểu của mình,
        /// đánh số nhát đúng bằng số hit, rồi về chỗ và camera lùi ra. Bốn kiểu ra đòn (xem AttackStyle):
        /// lao tới đánh, đứng yên bắn đạn, lao xuyên qua mục tiêu rồi đánh tiếp từ sau lưng, đứng yên ra đòn.
        /// Thẻ không nhắm vào phe đối diện (heal, shield) thì chỉ zoom và hiện hiệu quả, không lao đi.
        /// </summary>
        private IEnumerator AnimateAction(ActionRecord action)
        {
            string onTarget = action.Target != null ? " lên " + action.Target.Name : "";
            AddLog(action.Source.Name + " dùng [" + action.Name + "]" + onTarget + ".");
            Refresh();

            UnitWidget attacker = FindWidget(action.Source);
            UnitWidget mainTarget = action.Target != null ? FindWidget(action.Target) : null;
            if (attacker == null)
            {
                foreach (HitRecord record in action.Hits) ApplyHit(record);
                yield break;
            }

            Vector3 home = attacker.HomePosition;
            float direction = action.Source.IsPlayer ? 1f : -1f;
            bool isOffensive = mainTarget != null && action.Target.IsPlayer != action.Source.IsPlayer;
            AttackStyle style = action.Style;
            // Chỉ kiểu lao tới và kiểu đâm xuyên mới rời chỗ đứng; chỉ khi đã áp sát thì hit pierce mới xuyên qua được.
            bool travels = isOffensive && (style == AttackStyle.Lunge || style == AttackStyle.DashThrough);

            // 1. Zoom vào người ra đòn.
            yield return MoveCamera(FocusOn(home, ZoomDistance), ZoomTime);
            yield return new WaitForSeconds(ZoomHold);

            // 2. Kiểu rời chỗ: lao tới trước mặt mục tiêu, camera đi theo.
            //    Kiểu đứng yên: camera lùi ra vừa đủ thấy cả người ra đòn lẫn mục tiêu.
            Vector3 targetPosition = isOffensive ? mainTarget.HomePosition : home;
            Vector3 strikePosition = home;
            if (travels)
            {
                strikePosition = targetPosition - new Vector3(direction * StrikeGap, 0f, 0.05f);
                attacker.Body.sortingOrder = 300;
                StartCoroutine(MoveCamera(FocusOn((strikePosition + targetPosition) * 0.5f, ClashDistance), DashTime));
                yield return MoveActor(attacker, home, strikePosition, DashTime);
            }
            else if (isOffensive)
            {
                yield return MoveCamera(TwoShot(home, targetPosition), DashTime * 1.5f);
            }

            // 3. Mỗi hit là một nhát đánh riêng, và tự chọn động tác theo thuộc tính của nó:
            //    - Hit pierce khi đã áp sát: lướt xuyên qua mục tiêu sang phía bên kia rồi quay mặt lại.
            //      Mỗi hit pierce xuyên một lần, nên hai hit pierce liên tiếp sẽ xuyên qua rồi xuyên ngược về.
            //      Kiểu DashThrough thì hit nào cũng xuyên.
            //    - Hit khác: nhích về phía mục tiêu từ chỗ đang đứng rồi thu về.
            //    - Kiểu tầm xa: hit nào cũng bắn đạn hoặc gọi sét từ chỗ đứng.
            //    Mỗi hit phát clip riêng của nó (xem FindStrikeClip); tới sự kiện Strike của clip thì ra đòn.
            //    Hit chưa có clip thì dùng ảnh ra đòn (_attack) nếu có.
            float facing = direction;
            foreach (HitRecord record in action.Hits)
            {
                bool hitsOpponent = record.Receiver.IsPlayer != action.Source.IsPlayer;
                UnitWidget receiver = FindWidget(record.Receiver);
                if (!hitsOpponent)
                {
                    // Hit cho bản thân hoặc đồng đội (thủ, hồi máu): phát clip theo đúng thuộc tính nếu unit có,
                    // ví dụ Enemy_Bo_xuong_defence, và áp hiệu quả tại sự kiện Strike của clip.
                    AnimationClip supportClip = Resources.Load<AnimationClip>(
                        AnimationFolder + attacker.DataName + "_" + record.Hit.attribute.ToString().ToLowerInvariant());
                    if (supportClip != null)
                    {
                        float supportStart = Time.time;
                        PlayStrikeClip(attacker, supportClip);
                        yield return new WaitForSeconds(StrikeTimeOf(supportClip));
                        ApplyHit(record);
                        float rest = supportClip.length - ClipEndMargin - (Time.time - supportStart);
                        if (rest > 0f) yield return new WaitForSeconds(rest);
                        StopStrikeClip();
                    }
                    else
                    {
                        ApplyHit(record);
                    }
                    yield return new WaitForSeconds(HitPause);
                    continue;
                }

                AnimationClip strikeClip = isOffensive ? FindStrikeClip(attacker, style, record.Hit.attribute) : null;
                bool piercesThrough = travels
                    && (style == AttackStyle.DashThrough || record.Hit.attribute == HitAttribute.Pierce);

                // Lấy đà: clip chạy từ đầu tới sự kiện Strike.
                float clipStart = Time.time;
                if (strikeClip != null)
                {
                    PlayStrikeClip(attacker, strikeClip);
                    yield return new WaitForSeconds(StrikeTimeOf(strikeClip));
                }
                else if (isOffensive)
                {
                    SetAttackPose(attacker, true);
                }

                if (style == AttackStyle.Ranged && isOffensive && receiver != null)
                {
                    if (action.Effect == ImpactEffect.Lightning) yield return StrikeWithLightning(receiver, record);
                    else yield return FireProjectile(attacker, receiver, direction, record);
                }
                else if (piercesThrough)
                {
                    // Lướt xuyên qua mục tiêu, gây sát thương đúng lúc đi ngang qua, rồi dừng ở phía bên kia.
                    Vector3 through = targetPosition - new Vector3(0f, 0f, 0.05f);
                    Vector3 otherSide = targetPosition + new Vector3(facing * StrikeGap, 0f, -0.05f);
                    yield return MoveActor(attacker, strikePosition, through, PierceTime * 0.5f, false);
                    ApplyHit(record);
                    if (receiver != null) StartCoroutine(HitReaction(receiver));
                    yield return MoveActor(attacker, through, otherSide, PierceTime * 0.5f, false);
                    strikePosition = otherSide;
                    facing = -facing;
                    StartCoroutine(MoveCamera(FocusOn((otherSide + targetPosition) * 0.5f, ClashDistance), DashTime));
                }
                else
                {
                    // Nhát đánh thường: nhích về phía đối thủ rồi thu về. Kiểu đứng yên làm việc này ngay tại chỗ đứng.
                    Vector3 lunge = strikePosition + new Vector3(facing * LungeDistance, 0f, 0f);
                    yield return MoveActor(attacker, strikePosition, lunge, LungeTime);
                    ApplyHit(record);
                    if (receiver != null) StartCoroutine(HitReaction(receiver));
                    yield return MoveActor(attacker, lunge, strikePosition, LungeTime * 1.5f);
                }

                // Giữ tư thế ra đòn tới gần hết clip. Dừng sớm một chút để clip không kịp quay vòng về khung đầu.
                if (strikeClip != null)
                {
                    float remaining = strikeClip.length - ClipEndMargin - (Time.time - clipStart);
                    if (remaining > 0f) yield return new WaitForSeconds(remaining);
                    StopStrikeClip();
                }
                // Sau khi xuyên sang phía bên kia thì quay mặt lại phía mục tiêu, sẵn sàng cho nhát kế.
                bool turned = facing != direction;
                if (attacker.Body.flipX != turned) SetFacing(attacker, turned);
                yield return new WaitForSeconds(HitPause);
            }

            // 4. Về chỗ, camera lùi ra.
            StopStrikeClip();
            SetPose(attacker, attacker.IdleSprite, 1f);
            SetFacing(attacker, false);
            if (travels)
            {
                StartCoroutine(MoveCamera(CameraPosition, ZoomTime));
                yield return MoveActor(attacker, strikePosition, home, DashTime);
                attacker.Body.sortingOrder = attacker.BodyOrder;
            }
            yield return MoveCamera(CameraPosition, ZoomTime);
        }

        /// <summary>Một phát bắn của kiểu tầm xa: người bắn giật lùi nhẹ, viên đạn bay tới người nhận rồi mới gây sát thương.</summary>
        private IEnumerator FireProjectile(UnitWidget attacker, UnitWidget receiver, float direction, HitRecord record)
        {
            Vector3 home = attacker.HomePosition;
            Vector3 recoil = home - new Vector3(direction * RecoilDistance, 0f, 0f);
            Vector3 from = home + new Vector3(direction * attacker.Width * 0.5f, attacker.Height * 0.55f, 0f);
            Vector3 to = receiver.ActorRoot.transform.position + new Vector3(0f, receiver.Height * 0.5f, 0f);

            // Đạn là con của người bắn, để khi trận được dựng lại giữa chừng thì nó bị hủy theo.
            SpriteRenderer shot = CreateStageSprite("Projectile", glowSprite, 400, attacker.ActorRoot.transform);
            shot.color = ProjectileColor;
            shot.transform.localScale = Vector3.one * ProjectileSize;
            shot.transform.position = from;

            StartCoroutine(MoveActor(attacker, home, recoil, LungeTime));

            float duration = Mathf.Max(0.08f, Vector3.Distance(from, to) / ProjectileSpeed);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                shot.transform.position = Vector3.Lerp(from, to, time / duration);
                yield return null;
            }
            Destroy(shot.gameObject);

            ApplyHit(record);
            StartCoroutine(HitReaction(receiver));
            yield return MoveActor(attacker, recoil, home, LungeTime * 1.5f);
        }

        /// <summary>Một đòn sét: tia sét giáng từ trên cao xuống người nhận, chớp sáng và tia lửa bung ra, rồi mới tính sát thương.</summary>
        private IEnumerator StrikeWithLightning(UnitWidget receiver, HitRecord record)
        {
            Transform parent = receiver.ActorRoot.transform;
            Vector3 strikePoint = parent.position + new Vector3(0f, receiver.Height * 0.5f, 0f);

            StartCoroutine(CombatVfx.LightningBolt(parent, strikePoint, unlitMaterial));
            // Chờ tia sét hiện ra một nhịp rồi mới cho trúng đòn.
            yield return new WaitForSeconds(LightningHitDelay);

            CombatVfx.ImpactBurst(parent, strikePoint, glowParticleMaterial);
            ApplyHit(record);
            StartCoroutine(HitReaction(receiver));
            yield return new WaitForSeconds(LightningHold);
        }

        /// <summary>Quay mặt unit sang hướng ngược lại hoặc về hướng gốc, giữ hình vẫn canh giữa chỗ đứng.</summary>
        private static void SetFacing(UnitWidget widget, bool turned)
        {
            widget.Body.flipX = turned;
            FitBody(widget);
        }

        /// <summary>Vị trí camera để thấy cùng lúc hai điểm trên sân, dùng khi người ra đòn đứng yên tại chỗ.</summary>
        private static Vector3 TwoShot(Vector3 a, Vector3 b)
        {
            // 0.54 là nửa bề ngang nhìn thấy trên mỗi đơn vị khoảng cách, với góc nhìn 34 độ và màn hình 16:9.
            float separation = Mathf.Abs(a.x - b.x);
            float distance = Mathf.Max(ClashDistance, (separation * 0.5f + 1.6f) / 0.54f);
            return FocusOn((a + b) * 0.5f, distance);
        }

        /// <summary>Cập nhật HP đang hiển thị theo một hit, ghi nhật ký và hiện con số bay lên.</summary>
        private void ApplyHit(HitRecord record)
        {
            string receiverName = record.Receiver.Name;
            string floatText;
            Color floatColor;
            int floatSize = 44;

            switch (record.Hit.attribute)
            {
                case HitAttribute.Heal:
                    AddLog("   " + receiverName + " hồi " + record.Amount + " HP.");
                    floatText = "+" + record.Amount;
                    floatColor = new Color(0.5f, 1f, 0.55f);
                    break;
                case HitAttribute.Defence:
                    AddLog("   " + receiverName + " nhận " + record.Amount + " shield.");
                    floatText = "+" + record.Amount + " shield";
                    floatColor = new Color(0.6f, 0.83f, 1f);
                    floatSize = 34;
                    break;
                case HitAttribute.Support:
                    AddLog("   (support chưa có tác dụng)");
                    return;
                default:
                    float multiplier = record.Receiver.GetMultiplier(record.Hit.attribute);
                    string note = multiplier > 1f ? " — điểm yếu!" : multiplier < 1f ? " — kháng" : "";
                    string damage = record.Amount > 0 ? "mất " + record.Amount + " HP" : "được shield chặn hết";
                    AddLog("   " + receiverName + " " + damage + " (" + AttributeName(record.Hit.attribute) + ")" + note + ".");

                    floatText = record.Amount > 0 ? "-" + record.Amount : "Chặn";
                    floatColor = Color.white;
                    if (multiplier > 1f)
                    {
                        floatColor = new Color(1f, 0.85f, 0.2f);
                        floatSize = 58;
                    }
                    else if (multiplier < 1f)
                    {
                        floatColor = new Color(0.7f, 0.7f, 0.7f);
                        floatSize = 34;
                    }
                    break;
            }

            UnitWidget widget = FindWidget(record.Receiver);
            if (widget != null)
            {
                widget.DisplayHp = record.HpAfter;
                widget.DisplayShield = record.ShieldAfter;
                if (record.Died)
                {
                    widget.DisplayDead = true;
                    AddLog("<b>" + receiverName + " bị hạ gục.</b>");
                }
                SpawnFloatingText(widget, floatText, floatColor, floatSize);
            }
            RefreshWidgets();
        }

        /// <summary>Di chuyển unit trên sân. Tắt smooth để đi đều tốc độ, dùng khi nối nhiều đoạn thành một đường lướt liền.</summary>
        private IEnumerator MoveActor(UnitWidget widget, Vector3 from, Vector3 to, float duration, bool smooth = true)
        {
            Transform actor = widget.ActorRoot.transform;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float progress = time / duration;
                actor.position = Vector3.Lerp(from, to, smooth ? Mathf.SmoothStep(0f, 1f, progress) : progress);
                yield return null;
            }
            actor.position = to;
        }

        /// <summary>Di chuyển camera. Lệnh di chuyển mới sẽ thay lệnh đang chạy dở.</summary>
        private IEnumerator MoveCamera(Vector3 to, float duration)
        {
            int id = ++cameraMoveId;
            Transform cameraTransform = stageCamera.transform;
            Vector3 from = cameraTransform.position;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                if (id != cameraMoveId) yield break;
                cameraTransform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, time / duration));
                yield return null;
            }
            if (id == cameraMoveId) cameraTransform.position = to;
        }

        /// <summary>Vị trí camera để nhìn cận một điểm trên sân, giữ nguyên góc chếch xuống.</summary>
        private static Vector3 FocusOn(Vector3 groundPoint, float distance)
        {
            return new Vector3(groundPoint.x, groundPoint.y + 2.1f, groundPoint.z - distance);
        }

        /// <summary>Unit trúng đòn: đổi sang ảnh bị đánh nếu có, nháy đỏ và rung nhẹ, rồi về ảnh đứng.</summary>
        private IEnumerator HitReaction(UnitWidget widget)
        {
            // Unit đang tự ra đòn (đang phát clip) thì giữ nguyên tư thế, chỉ rung.
            bool showHurt = widget.HurtSprite != null && !widget.ClipPlaying;
            int token = ++widget.HurtToken;
            if (showHurt) SetPose(widget, widget.HurtSprite, HurtHeightRatio);

            Transform body = widget.Body.transform;
            for (float time = 0f; time < HitShakeTime; time += Time.deltaTime)
            {
                body.localPosition = widget.BodyRestPosition + new Vector3(
                    Random.Range(-HitShakeAmount, HitShakeAmount),
                    Random.Range(-HitShakeAmount, HitShakeAmount) * 0.5f,
                    0f);
                widget.Body.color = HitFlashColor;
                yield return null;
            }
            body.localPosition = widget.BodyRestPosition;
            RefreshWidgets();

            if (!showHurt) yield break;
            yield return new WaitForSeconds(HurtHold);
            // Chỉ nhát trúng mới nhất mới được đổi ảnh về.
            if (token == widget.HurtToken && widget.Body.sprite == widget.HurtSprite)
                SetPose(widget, widget.IdleSprite, 1f);
        }

        private void SpawnFloatingText(UnitWidget widget, string content, Color color, int size)
        {
            Text text = UiFactory.Label(floatRoot, "Float", size, TextAnchor.MiddleCenter, 0f);
            text.text = content;
            text.color = color;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(320f, 80f);

            Vector3 anchor = widget.ActorRoot.transform.position + new Vector3(0f, widget.Height * 0.8f, 0f);
            StartCoroutine(FloatText(text, anchor));
        }

        private IEnumerator FloatText(Text text, Vector3 worldAnchor)
        {
            Color color = text.color;
            for (float time = 0f; time < FloatTextTime; time += Time.deltaTime)
            {
                float progress = time / FloatTextTime;
                text.rectTransform.anchoredPosition = WorldToCanvas(worldAnchor) + new Vector2(0f, 110f * progress);
                color.a = 1f - progress * progress;
                text.color = color;
                yield return null;
            }
            Destroy(text.gameObject);
        }

        private UnitWidget FindWidget(Unit unit)
        {
            foreach (UnitWidget widget in widgets)
            {
                if (widget.Unit == unit) return widget;
            }
            return null;
        }

        // ---------- Vẽ lại ----------

        private void Refresh()
        {
            if (battle == null) return;

            RefreshOrderBar();
            RefreshWidgets();
            // Đang kéo thì không dựng lại tay bài, vì làm vậy sẽ hủy chính thẻ đang được kéo.
            if (!isDragging) RefreshHand();
            RefreshInfo();
            endTurnButton.interactable = CanAct;
            logText.text = string.Join("\n", logLines);
        }

        private void RefreshOrderBar()
        {
            var text = new StringBuilder();
            text.Append("Vòng ").Append(battle.Round);
            if (displayedCurrent != null) text.Append("   |   Đang đi: <b>").Append(displayedCurrent.Name).Append("</b>");

            List<Unit> upcoming = battle.UpcomingTurns(UpcomingTurnsShown);
            if (upcoming.Count > 0)
            {
                text.Append("   |   Kế tiếp: ");
                for (int i = 0; i < upcoming.Count; i++)
                {
                    if (i > 0) text.Append(" → ");
                    text.Append(upcoming[i].Name);
                }
            }
            orderText.text = text.ToString();
        }

        private void RefreshWidgets()
        {
            CardData selected = SelectedCard;
            foreach (UnitWidget widget in widgets) RefreshWidget(widget, selected);
        }

        private void RefreshWidget(UnitWidget widget, CardData selected)
        {
            Unit unit = widget.Unit;
            bool alive = !widget.DisplayDead;

            widget.Panel.gameObject.SetActive(alive);
            widget.HitArea.gameObject.SetActive(alive);
            widget.Body.color = alive ? widget.BaseColor : widget.BaseColor * DeadTint;

            if (!alive)
            {
                widget.Ring.enabled = false;
                widget.Glow.SetActive(false);
                return;
            }

            var text = new StringBuilder();
            text.Append("<b><size=20>").Append(unit.Name).Append("</size></b>\n");
            text.Append("HP ").Append(widget.DisplayHp).Append("/").Append(unit.MaxHp);
            if (widget.DisplayShield > 0) text.Append("  <color=#9CD3FF>Shield ").Append(widget.DisplayShield).Append("</color>");
            if (unit.EnemyData != null)
            {
                text.Append("\n").Append(WeaknessText(unit.EnemyData));
                text.Append("\n<color=#FFD966>").Append(IntentText(unit.Intent)).Append("</color>");
            }
            widget.Text.text = text.ToString();
            widget.HpFill.anchorMax = new Vector2(Mathf.Clamp01(widget.DisplayHp / (float)unit.MaxHp), 1f);

            // Vòng sáng dưới chân: xanh lá cho mục tiêu hợp lệ của thẻ đang kéo, vàng cho unit đang tới turn.
            bool isValidTarget = selected != null && battle.IsValidTarget(selected, unit);
            bool isCurrent = displayedCurrent == unit;
            // Unit sẽ chịu tác động của thẻ đang kéo: một lớp ánh sáng ôm sát viền hình và vòng sáng dưới chân,
            // đỏ cam nếu bị đánh, xanh lam nếu được hỗ trợ.
            bool isHighlighted = highlighted.Contains(unit);
            widget.Glow.SetActive(isHighlighted);
            if (isHighlighted)
            {
                bool isHelped = displayedCurrent != null && unit.IsPlayer == displayedCurrent.IsPlayer;
                Color highlight = isHelped ? AllyHighlightColor : EnemyHighlightColor;
                widget.GlowColor = highlight;
                SetGlow(widget, highlight, 1f);
                widget.Ring.enabled = true;
                widget.Ring.color = highlight;
            }
            else if (isValidTarget)
            {
                widget.Ring.enabled = true;
                widget.Ring.color = TargetRingColor;
            }
            else if (isCurrent)
            {
                widget.Ring.enabled = true;
                widget.Ring.color = CurrentRingColor;
            }
            else
            {
                widget.Ring.enabled = false;
            }
        }

        private void RefreshHand()
        {
            // Gỡ thẻ cũ khỏi layout ngay, vì Destroy chỉ có hiệu lực cuối frame.
            for (int i = handRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = handRoot.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            // Chỉ hiện tay bài khi sân đã diễn tới đúng turn của nhân vật đó.
            if (!IsPlayerTurn || displayedCurrent != battle.Current) return;

            Unit current = battle.Current;
            IReadOnlyList<CardData> hand = current.Deck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                bool affordable = card.apCost <= current.Ap;
                Color color = i == selectedIndex ? CardSelectedColor : affordable ? CardColor : CardDisabledColor;

                Button button = UiFactory.MakeButton(handRoot, card.displayName, color, 20, TextAnchor.UpperLeft, out Text label);
                ((RectTransform)button.transform).sizeDelta = new Vector2(CardWidth, CardArtHeight + CardTextHeight);
                button.transition = Selectable.Transition.None;
                label.text = CardText(card);
                if (!affordable) label.color = new Color(1f, 1f, 1f, 0.45f);
                // Chữ nằm dưới ảnh minh họa.
                label.rectTransform.offsetMax = new Vector2(-10f, -(CardArtHeight + 6f));
                AddCardArt(button.transform, card, affordable);

                button.gameObject.AddComponent<CanvasGroup>();
                CardDragHandler drag = button.gameObject.AddComponent<CardDragHandler>();
                drag.Screen = this;
                drag.HandIndex = i;
            }
        }

        /// <summary>Đặt ảnh minh họa lên nửa trên của thẻ. Thẻ chưa có ảnh thì để một ô tối thế chỗ.</summary>
        private static void AddCardArt(Transform cardRoot, CardData card, bool affordable)
        {
            var artObject = new GameObject("Art", typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(cardRoot, false);
            var rect = (RectTransform)artObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, CardArtHeight);

            Image image = artObject.GetComponent<Image>();
            image.raycastTarget = false;
            Sprite art = Resources.Load<Sprite>(CardArtFolder + card.name);
            if (art == null)
            {
                image.color = new Color(0f, 0f, 0f, 0.35f);
                return;
            }
            image.sprite = art;
            // Thẻ không đủ AP thì ảnh tối đi, giống phần chữ.
            image.color = affordable ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        private void RefreshInfo()
        {
            var text = new StringBuilder();
            if (IsPlayerTurn && displayedCurrent == battle.Current)
            {
                Unit current = battle.Current;
                text.Append("<b>").Append(current.Name).Append("</b>\n");
                text.Append("AP: ").Append(current.Ap).Append("/").Append(current.MaxAp).Append("\n");
                text.Append("ATK ").Append(current.Attack)
                    .Append("  DEF ").Append(current.Defence)
                    .Append("  SPD ").Append(current.Speed).Append("\n");
                text.Append("Chồng rút: ").Append(current.Deck.DrawPile.Count).Append("\n");
                text.Append("Chồng bỏ: ").Append(current.Deck.DiscardPile.Count).Append("\n\n");
                text.Append("<size=20>Kéo thẻ rồi thả vào mục tiêu.</size>");
            }
            else if (battle.Outcome == BattleOutcome.Ongoing)
            {
                text.Append("Kẻ địch đang hành động...");
            }
            infoText.text = text.ToString();
        }

        // ---------- Chữ hiển thị ----------

        private void AddLog(string line)
        {
            logLines.Add(line);
            while (logLines.Count > MaxLogLines) logLines.RemoveAt(0);
            dirty = true;
        }

        private static string CardText(CardData card)
        {
            var text = new StringBuilder();
            text.Append("<b><size=26>").Append(card.displayName).Append("</size></b>\n");
            text.Append(card.rate).Append("   |   AP ").Append(card.apCost).Append("\n\n");
            foreach (HitData hit in card.hits) text.Append(HitText(hit)).Append("\n");
            text.Append("\n<size=17><i>").Append(card.description).Append("</i></size>");
            return text.ToString();
        }

        private static string HitText(HitData hit)
        {
            var text = new StringBuilder();
            text.Append(AttributeName(hit.attribute)).Append(" ").Append(hit.value);
            if (hit.status != StatusType.None)
            {
                text.Append(" + ").Append(hit.status.ToString().ToLowerInvariant()).Append(" ").Append(hit.statusAmount);
            }
            switch (hit.target)
            {
                case HitTarget.Self: text.Append(" (bản thân)"); break;
                case HitTarget.AllAllies: text.Append(" (cả đội)"); break;
                case HitTarget.AllEnemies: text.Append(" (mọi địch)"); break;
            }
            return text.ToString();
        }

        private static string WeaknessText(EnemyData data)
        {
            if (data.weaknesses.Count == 0) return "Không có điểm yếu";
            var text = new StringBuilder();
            for (int i = 0; i < data.weaknesses.Count; i++)
            {
                WeaknessEntry entry = data.weaknesses[i];
                if (i > 0) text.Append("  ");
                string color = entry.multiplier > 1f ? "#8CFF8C" : entry.multiplier < 1f ? "#FF9C9C" : "#FFFFFF";
                text.Append("<color=").Append(color).Append(">")
                    .Append(AttributeName(entry.attribute)).Append(" x").Append(entry.multiplier.ToString("0.#"))
                    .Append("</color>");
            }
            return text.ToString();
        }

        private static string IntentText(EnemyIntent intent)
        {
            if (intent == null || intent.Move == null) return "Ý định: không rõ";
            var text = new StringBuilder("Ý định: ").Append(intent.Move.displayName);
            if (intent.Target != null) text.Append(" → ").Append(intent.Target.Name);
            if (intent.Move.hits.Count > 0)
            {
                text.Append(" (");
                for (int i = 0; i < intent.Move.hits.Count; i++)
                {
                    if (i > 0) text.Append(", ");
                    text.Append(HitText(intent.Move.hits[i]));
                }
                text.Append(")");
            }
            return text.ToString();
        }

        private static string AttributeName(HitAttribute attribute)
        {
            return attribute.ToString().ToLowerInvariant();
        }
    }
}
