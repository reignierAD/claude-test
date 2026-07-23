using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SuikodenLike.Data;

namespace SuikodenLike.Dialogue
{
    /// <summary>
    /// The visual half of the dialogue system. Wire the fields to your Canvas
    /// in the editor. DialogueManager drives it. Uses TextMeshPro.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] GameObject root;                 // panel to show/hide
        [SerializeField] TMP_Text speakerText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Image portraitImage;
        [SerializeField] GameObject continueIndicator;    // little blinking arrow

        [Header("Choices")]
        [SerializeField] Transform choicesContainer;
        [SerializeField] Button choiceButtonPrefab;

        readonly List<Button> spawnedChoices = new List<Button>();

        void Awake() => Hide();

        public void Show() { if (root) root.SetActive(true); }

        public void Hide()
        {
            if (root) root.SetActive(false);
            ClearChoices();
        }

        public void SetLine(string speaker, string body, Sprite portrait)
        {
            if (speakerText) speakerText.text = speaker ?? "";
            if (bodyText) bodyText.text = body ?? "";
            if (portraitImage)
            {
                portraitImage.enabled = portrait != null;
                portraitImage.sprite = portrait;
            }
        }

        public void SetBody(string body) { if (bodyText) bodyText.text = body ?? ""; }

        public void SetContinueIndicator(bool on)
        {
            if (continueIndicator) continueIndicator.SetActive(on);
        }

        public void ShowChoices(List<DialogueChoice> choices, Action<int> onPicked)
        {
            ClearChoices();
            if (choicesContainer == null || choiceButtonPrefab == null) return;

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label) label.text = choices[i].text;
                btn.onClick.AddListener(() => onPicked?.Invoke(index));
                spawnedChoices.Add(btn);
            }
        }

        public void ClearChoices()
        {
            foreach (var b in spawnedChoices) if (b) Destroy(b.gameObject);
            spawnedChoices.Clear();
        }
    }
}
