using System;
using System.Collections.Generic;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    public enum ChapterNodeKind { Battle, Elite, Event, Rest, Shop, Boss }
    [Serializable] public class ChapterChoice
    {
        public string label;
        [TextArea] public string consequence;
        public int heal, credits, noise, memory;
        public CardTemplate card;
    }
    [Serializable] public class ChapterNode
    {
        public string id, title;
        [TextArea(3, 8)] public string story;
        public int depth;
        public ChapterNodeKind kind;
        public EchoArtRegion region;
        public EnemyTemplate enemy;
        public Sprite background;
        public int credits = 20;
        public List<ChapterChoice> choices = new List<ChapterChoice>();
    }
    [CreateAssetMenu(menuName = "Variables Echo/Chapter")]
    public class EchoChapterData : ScriptableObject
    {
        public string chapterTitle = "第一章  镜像中继站";
        public HeroTemplate hero;
        public HeroTemplate alternateHero;
        public List<CardTemplate> cards = new List<CardTemplate>();
        public List<CardTemplate> rewards = new List<CardTemplate>();
        public List<ChapterNode> nodes = new List<ChapterNode>();
        public int maxDepth = 8, restHeal = 22, removePrice = 40, cardPrice = 35;
        public CardTemplate FindCard(int id) { return cards.Find(c => c != null && c.Id == id); }
    }
    [Serializable] public class ChapterSave
    {
        public int version = 1, seed, depth, hp = 80, credits = 30, memory, startingNoise, heroVariant;
        public string phase = "Map", nodeId, ending;
        public bool shopBought, shopRemoved;
        public List<int> deck = new List<int>();
        public List<int> reward = new List<int>();
        public List<string> journal = new List<string>();
    }
}


