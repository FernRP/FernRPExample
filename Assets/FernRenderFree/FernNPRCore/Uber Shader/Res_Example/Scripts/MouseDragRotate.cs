using UnityEngine;

namespace FernNPRCore.Scripts
{
    public class MouseDragRotate : MonoBehaviour
    {
        public float speed = 10;

        private float OffsetX;
        private float OffsetY;

        private Vector3 startPos;
        private Vector3 nowPos;

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                OffsetX = Input.GetAxis("Mouse X");
                OffsetY = Input.GetAxis("Mouse Y");

                transform.Rotate(new Vector3(0, -OffsetX, 0) * speed, Space.World);
            }
        }
    }
}