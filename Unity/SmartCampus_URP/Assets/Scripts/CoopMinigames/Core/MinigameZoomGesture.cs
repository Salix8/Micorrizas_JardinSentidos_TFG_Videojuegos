using UnityEngine;

namespace SmartCampus.Coop.Minigames
{
    /// <summary>
    /// Estado global del gesto de zoom con dos dedos. Mientras esta activo (desde que se apoya el
    /// segundo dedo hasta que se levantan todos) ningun toque cuenta como jugada.
    /// </summary>
    public static class MinigameZoomGesture
    {
        private const float InteractionCooldownSeconds = 0.15f;

        private static float lastEndTime = -10f;

        public static bool IsActive { get; private set; }

        /// <summary>Se incrementa en cada gesto. Sirve para saber si hubo zoom durante un arrastre.</summary>
        public static int SessionId { get; private set; }

        /// <summary>True durante el gesto y un instante despues, para que soltar los dedos no pulse nada.</summary>
        public static bool BlocksInteraction => IsActive || Time.unscaledTime - lastEndTime < InteractionCooldownSeconds;

        internal static void Begin()
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;
            SessionId++;
        }

        internal static void End()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            lastEndTime = Time.unscaledTime;
        }

        /// <summary>Uso desde arrastres: true si el gesto de zoom invalida el arrastre iniciado en <paramref name="dragSessionId"/>.</summary>
        public static bool InterruptedDrag(int dragSessionId) => IsActive || SessionId != dragSessionId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsActive = false;
            SessionId = 0;
            lastEndTime = -10f;
        }
    }
}
