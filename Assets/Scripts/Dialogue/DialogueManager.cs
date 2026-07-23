using System.Collections;
using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;
using SuikodenLike.Field;

namespace SuikodenLike.Dialogue
{
    /// <summary>
    /// Runs conversations: typewriter text, advancing on button press,
    /// branching choices, and per-line side effects (give item / recruit /
    /// set flag). Locks player control while a conversation is open.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [SerializeField] DialogueUI ui;
        [SerializeField] float charsPerSecond = 45f;
        [SerializeField] KeyCode advanceKey = KeyCode.E;

        public bool IsRunning { get; private set; }

        int pickedChoice = -1;
        bool skipTypewriter;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartDialogue(DialogueData data, PlayerController player)
        {
            if (IsRunning || data == null || data.lines.Count == 0) return;
            StartCoroutine(RunDialogue(data, player));
        }

        /// <summary>Quick one-off message with no speaker (chests, signs, system text).</summary>
        public void ShowSystemLine(string text, PlayerController player)
        {
            if (IsRunning) return;
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.lines.Add(new DialogueLine { speakerName = "", text = text });
            StartCoroutine(RunDialogue(data, player));
        }

        IEnumerator RunDialogue(DialogueData data, PlayerController player)
        {
            IsRunning = true;
            if (player) player.ControlEnabled = false;
            ui.Show();

            int index = 0;
            while (index >= 0 && index < data.lines.Count)
            {
                DialogueLine line = data.lines[index];
                ApplySideEffects(line);

                ui.SetContinueIndicator(false);
                ui.SetLine(line.speakerName, "", line.portrait);
                yield return StartCoroutine(Typewriter(line.text));

                if (line.choices != null && line.choices.Count > 0)
                {
                    pickedChoice = -1;
                    ui.ShowChoices(line.choices, i => pickedChoice = i);
                    while (pickedChoice < 0) yield return null;
                    ui.ClearChoices();

                    int gotoLine = line.choices[pickedChoice].gotoLine;
                    index = gotoLine; // -1 ends the conversation
                    continue;
                }

                ui.SetContinueIndicator(true);
                yield return WaitForAdvance();
                index++;
            }

            ui.Hide();
            if (player) player.ControlEnabled = true;
            IsRunning = false;
        }

        IEnumerator Typewriter(string text)
        {
            skipTypewriter = false;
            if (charsPerSecond <= 0f) { ui.SetBody(text); yield break; }

            var buffer = new System.Text.StringBuilder();
            float delay = 1f / charsPerSecond;
            foreach (char c in text)
            {
                if (skipTypewriter) { ui.SetBody(text); yield break; }
                buffer.Append(c);
                ui.SetBody(buffer.ToString());
                if (AdvancePressed()) { skipTypewriter = true; }
                yield return new WaitForSeconds(delay);
            }
        }

        IEnumerator WaitForAdvance()
        {
            // Require a fresh press so we don't consume the same keydown twice.
            yield return null;
            while (!AdvancePressed()) yield return null;
        }

        bool AdvancePressed() =>
            Input.GetKeyDown(advanceKey) || Input.GetButtonDown("Submit") ||
            Input.GetMouseButtonDown(0);

        void ApplySideEffects(DialogueLine line)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (line.giveItem != null) gm.Inventory.Add(line.giveItem, 1);
            if (line.recruitCharacter != null) gm.RecruitCharacter(line.recruitCharacter);
            if (!string.IsNullOrEmpty(line.setFlag)) gm.SetFlag(line.setFlag);
        }
    }
}
