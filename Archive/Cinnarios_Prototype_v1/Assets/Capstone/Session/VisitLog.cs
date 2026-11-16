using System;
using System.Collections.Generic;
using System.IO;
using Capstone.Ranking;

namespace Capstone.Session
{
    /// <summary>
    /// The lines of one visit. An invalid code writes nothing.
    /// </summary>
    public sealed class VisitLog
    {
        readonly List<string> lines = new List<string>();
        bool open;

        public bool IsOpen => open;

        public bool Open(string code)
        {
            lines.Clear();
            open = false;
            if (!EntranceCodes.Accepts(code))
                return false;

            open = true;
            lines.Add("code " + code.Trim().ToUpperInvariant());
            return true;
        }

        public void Question(string question, string reply)
        {
            if (!open)
                return;

            lines.Add("question " + (question ?? "") + " | " + (reply ?? ""));
        }

        public void Decide(string workId, Decision decision)
        {
            if (!open)
                return;

            lines.Add("decision " + decision.ToString().ToLowerInvariant() + " " + (workId ?? ""));
        }

        public string Text => open ? string.Join("\n", lines.ToArray()) : "";

        public bool Write(string path)
        {
            if (!open || string.IsNullOrWhiteSpace(path))
                return false;

            File.WriteAllText(path, Text);
            return true;
        }
    }
}
