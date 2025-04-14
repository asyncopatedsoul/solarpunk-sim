using UnityEngine;

namespace ECC
{
    public class BaseModule : ScriptableObject
    {
        [Header("Basic")]
        public string Name;
        public string Description;
        public Sprite Representation;

        [Header("Custom")]
        public string CustomString;
        public float CustomFloat;
        public int CustomInt;
    }
}
