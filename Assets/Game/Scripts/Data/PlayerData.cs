using System.Collections.Generic;
using UnityEngine;

namespace COYGame
{
    [CreateAssetMenu(menuName = "COY/Player", fileName = "NewPlayer")]
    public sealed class PlayerData : ScriptableObject
    {
        public string playerName;
        [Min(0)] public int attack;
        [Min(0)] public int defense;
        public Sprite cardArtwork;
        public Color placeholderColor = Color.white;
        public List<CardData> attackCards = new();
        public List<CardData> defenseCards = new();
    }
}
