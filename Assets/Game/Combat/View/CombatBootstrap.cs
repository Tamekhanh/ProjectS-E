using UnityEngine;

namespace ProjectS.Combat.View
{
    /// <summary>
    /// Tự dựng màn combat thử khi bấm Play, không cần đặt gì vào scene.
    /// TẠM: khi game có menu và cốt truyện, đổi AutoStart thành false và mở combat từ luồng game.
    /// </summary>
    public static class CombatBootstrap
    {
        public static readonly bool AutoStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartCombatScreen()
        {
            if (!AutoStart) return;
            new GameObject("CombatScreen").AddComponent<CombatScreen>();
        }
    }
}
