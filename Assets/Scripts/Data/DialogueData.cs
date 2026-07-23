using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuikodenLike.Data
{
    [Serializable]
    public class DialogueChoice
    {
        public string text = "...";
        [Tooltip("Index of the line to jump to when this choice is picked. -1 = end.")]
        public int gotoLine = -1;
    }

    [Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [Tooltip("Optional portrait override. Falls back to the speaker's default.")]
        public Sprite portrait;
        [TextArea(2, 5)] public string text;

        [Tooltip("Optional branching choices. If empty, advances to the next line.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Header("Optional side effects")]
        [Tooltip("Give this item to the player when the line is shown.")]
        public ItemData giveItem;
        [Tooltip("Recruit this character into the party when shown.")]
        public CharacterData recruitCharacter;
        [Tooltip("Set a story flag (see GameManager flags) when shown.")]
        public string setFlag;
    }

    /// <summary>
    /// A full conversation. Author your whole story's dialogue as these
    /// assets and hook them onto NPCs / triggers — no code changes.
    /// Create via: Assets > Create > SuikodenLike > Dialogue.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "SuikodenLike/Dialogue")]
    public class DialogueData : ScriptableObject
    {
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}
