using Capstone.App.Data;
using Capstone.App.Services;
using Capstone.App.UI;
using UnityEngine;

/// <summary>
/// "Before Visit": the four onboarding questions, one per intro slide (extra ones share the last slide).
/// A slide with a question waits for the answer instead of a tap, then slides on with its own animation.
/// After the last answer the card says "Got it—tailoring your gallery experience..." for 1.5 s.
/// The questions are asked on every entrance; each entrance is a new session.
/// </summary>
public sealed class PreVisitSurvey
{
    const float ConfirmSeconds = 1.5f;

    readonly StudyQuestion[] questions = StudyQuestions.Before;
    readonly int panelCount;
    readonly bool[] done;
    QuestionCardView card;
    RectTransform cardPanel;
    bool checkedEarlier;
    bool alreadyGiven;
    bool advance;
    bool confirming;

    public PreVisitSurvey(int panelCount)
    {
        this.panelCount = Mathf.Max(1, panelCount);
        done = new bool[questions.Length];
    }

    /// <summary>True while this slide still has a question (or its answer is still being confirmed).</summary>
    public bool Asks(int panel)
    {
        CheckEarlierAnswers();
        return !alreadyGiven && (advance || confirming || NextUnanswered(panel) >= 0);
    }

    /// <summary>Shows this slide's next question if it is not already showing. Safe to call every frame.</summary>
    public void ShowOn(RectTransform panel, int index)
    {
        CheckEarlierAnswers();
        if (alreadyGiven || panel == null || advance || confirming)
            return;
        if (card != null && cardPanel == panel)
            return;

        int next = NextUnanswered(index);
        if (next < 0)
            return;

        if (card != null)
            Object.Destroy(card.gameObject);
        cardPanel = panel;
        StudyQuestion asked = questions[next];
        card = QuestionCardView.Show(panel, asked, null, next + 1, questions.Length,
            answer => Answered(panel, index, next, answer),
            onMany: picks => SurveyDataManager.Ensure().RecordMany(asked, picks));
    }

    /// <summary>Returns true once, after the slide's last question is answered.</summary>
    public bool TakeAdvance()
    {
        if (!advance)
            return false;
        advance = false;
        return true;
    }

    void Answered(RectTransform panel, int index, int question, int answer)
    {
        SurveyDataManager data = SurveyDataManager.Ensure();
        data.Record(questions[question], answer);
        done[question] = true;

        if (System.Array.TrueForAll(done, d => d))
        {
            data.SaveBefore();
            confirming = true;
            card.ShowMessage(StudyQuestions.Tailoring, ConfirmSeconds, () =>
            {
                confirming = false;
                Close();
                advance = true;
            });
            return;
        }

        Close();
        if (NextUnanswered(index) >= 0)
            ShowOn(panel, index);
        else
            advance = true;
    }

    void Close()
    {
        if (card != null)
            UIMotion.Dismiss(card.gameObject);
        card = null;
        cardPanel = null;
    }

    int NextUnanswered(int panel)
    {
        int first = Mathf.Min(panel, questions.Length);
        int last = panel >= panelCount - 1 ? questions.Length - 1 : Mathf.Min(panel, questions.Length - 1);
        for (int i = first; i <= last; i++)
        {
            if (!done[i])
                return i;
        }

        return -1;
    }

    void CheckEarlierAnswers()
    {
        if (checkedEarlier)
            return;
        checkedEarlier = true;
        // Every entrance starts a fresh session, so the questions are always asked. The "after" answers
        // pair with the newest open session for that code.
        alreadyGiven = false;
    }
}
