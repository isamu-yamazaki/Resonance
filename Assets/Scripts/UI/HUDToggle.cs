using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.UI
{
    public class HUDToggle : MonoBehaviour
    {
        [SerializeField] private GameObject[] hudRoots;
        [SerializeField] private Key toggleKey = Key.H;

        private bool _hudVisible = true;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                SetHUDVisible(!_hudVisible);
        }

        private void SetHUDVisible(bool visible)
        {
            _hudVisible = visible;
            foreach (var root in hudRoots)
            {
                if (root != null)
                    root.SetActive(visible);
            }
        }
    }
}
