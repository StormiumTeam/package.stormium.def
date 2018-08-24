using UnityEngine;

namespace package.stormium.def.characters
{
    public class GameCharacter : MonoBehaviour
    {
        [Tooltip("Used when seeing the object (3rd P or not camera target)")]
        public GameObject OutsideGameObject;
        [Tooltip("Used when the camera is inside the object (target 1st P)")]
        public GameObject InsideGameObject;
    }
}