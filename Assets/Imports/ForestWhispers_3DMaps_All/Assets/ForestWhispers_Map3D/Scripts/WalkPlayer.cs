using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ForestWhispers
{
    /// Simple walker for checking a map. WASD to move, Shift to run, Space to jump, hold the right mouse button to look.
    [RequireComponent(typeof(CharacterController))]
    public class WalkPlayer : MonoBehaviour
    {
        public float walk = 4.6f, run = 9f, jump = 5f, gravity = 16f, lookSpeed = 2.2f;
        public Transform head;
        CharacterController cc; float vy, yaw, pitch;

        void Start() { cc = GetComponent<CharacterController>(); yaw = transform.eulerAngles.y; }

        void Update()
        {
            Vector2 move, look; bool running, jumping, looking;
            Read(out move, out look, out running, out jumping, out looking);
            if (looking)
            {
                yaw += look.x * lookSpeed; pitch = Mathf.Clamp(pitch - look.y * lookSpeed, -70f, 70f);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (head != null) head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            Vector3 v = (transform.forward * move.y + transform.right * move.x) * (running ? run : walk);
            if (cc.isGrounded) { vy = jumping ? jump : -2f; } else { vy -= gravity * Time.deltaTime; }
            v.y = vy;
            cc.Move(v * Time.deltaTime);
        }

        void Read(out Vector2 move, out Vector2 look, out bool running, out bool jumping, out bool looking)
        {
            move = Vector2.zero; look = Vector2.zero; running = false; jumping = false; looking = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            running = Input.GetKey(KeyCode.LeftShift); jumping = Input.GetKeyDown(KeyCode.Space); looking = Input.GetMouseButton(1);
#elif ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current;
            if (k != null)
            {
                move.x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
                move.y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
                running = k.leftShiftKey.isPressed; jumping = k.spaceKey.wasPressedThisFrame;
            }
            if (m != null) { look = m.delta.ReadValue() * 0.05f; looking = m.rightButton.isPressed; }
#endif
        }
    }
}
