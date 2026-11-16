using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Capstone.App.Data
{
    /// <summary>
    /// One buyer's paired before/after assessment. Ratings are 1-7 and 0 means "not answered";
    /// the export writes 0 as null. Deltas are after minus before, ready for a paired t-test.
    /// </summary>
    [Serializable]
    public sealed class StudySession
    {
        public string session_id;
        public string user_id;
        public string pre_timestamp;
        public string post_timestamp;
        public int pre_confidence;
        public int post_confidence;
        public int pre_size_clarity;
        public int post_size_clarity;
        public string pre_blocker;
        public string post_blocker_resolution;
        public string pre_intent;
        public string post_helpful_feature;
        public List<string> outlier_tags = new List<string>();
        public string optional_comment;

        public static StudySession Start(string user)
        {
            return new StudySession { session_id = Guid.NewGuid().ToString(), user_id = user };
        }

        public bool HasBefore => !string.IsNullOrEmpty(pre_timestamp);
        public bool HasAfter => !string.IsNullOrEmpty(post_timestamp);

        public int? ConfidenceDelta => Delta(pre_confidence, post_confidence);
        public int? SizeClarityDelta => Delta(pre_size_clarity, post_size_clarity);

        public static int? Delta(int before, int after)
        {
            if (before < 1 || after < 1)
                return null;
            return after - before;
        }

        public static string Now() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        /// <summary>The flat export object, in the agreed field order.</summary>
        public string ToJson()
        {
            var json = new StringBuilder("{");
            Field(json, "session_id", Str(session_id), true);
            Field(json, "user_id", Str(user_id));
            Field(json, "pre_timestamp", Str(pre_timestamp));
            Field(json, "post_timestamp", Str(post_timestamp));
            Field(json, "pre_confidence", Num(pre_confidence));
            Field(json, "post_confidence", Num(post_confidence));
            Field(json, "confidence_delta", Num(ConfidenceDelta));
            Field(json, "pre_size_clarity", Num(pre_size_clarity));
            Field(json, "post_size_clarity", Num(post_size_clarity));
            Field(json, "size_clarity_delta", Num(SizeClarityDelta));
            Field(json, "pre_blocker", Str(pre_blocker));
            Field(json, "post_blocker_resolution", Str(post_blocker_resolution));
            Field(json, "pre_intent", Str(pre_intent));
            Field(json, "post_helpful_feature", Str(post_helpful_feature));
            var tags = new StringBuilder("[");
            for (int i = 0; i < outlier_tags.Count; i++)
                tags.Append(i > 0 ? "," : "").Append(Str(outlier_tags[i]));
            Field(json, "outlier_tags", tags.Append(']').ToString());
            Field(json, "optional_comment", Str(optional_comment));
            return json.Append('}').ToString();
        }

        public static readonly string CsvHeader =
            "session_id,user_id,pre_timestamp,post_timestamp,pre_confidence,post_confidence,confidence_delta," +
            "pre_size_clarity,post_size_clarity,size_clarity_delta,pre_blocker,post_blocker_resolution,pre_intent," +
            "post_helpful_feature,outlier_tags,optional_comment";

        /// <summary>The same fields as one CSV line, for Excel. Tags are joined with "; ".</summary>
        public string ToCsv()
        {
            string[] cells =
            {
                session_id, user_id, pre_timestamp, post_timestamp,
                Cell(pre_confidence), Cell(post_confidence), Cell(ConfidenceDelta),
                Cell(pre_size_clarity), Cell(post_size_clarity), Cell(SizeClarityDelta),
                pre_blocker, post_blocker_resolution, pre_intent, post_helpful_feature,
                string.Join("; ", outlier_tags), optional_comment
            };
            for (int i = 0; i < cells.Length; i++)
                cells[i] = Quote(cells[i]);
            return string.Join(",", cells);
        }

        static void Field(StringBuilder json, string name, string value, bool first = false)
        {
            if (!first)
                json.Append(',');
            json.Append('"').Append(name).Append("\":").Append(value);
        }

        static string Num(int value) => value >= 1 ? value.ToString(CultureInfo.InvariantCulture) : "null";
        static string Num(int? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "null";
        static string Cell(int value) => value >= 1 ? value.ToString(CultureInfo.InvariantCulture) : "";
        static string Cell(int? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";

        static string Str(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "null";
            var text = new StringBuilder("\"");
            foreach (char c in value)
            {
                if (c == '"' || c == '\\')
                    text.Append('\\').Append(c);
                else if (c == '\n')
                    text.Append("\\n");
                else if (c >= ' ')
                    text.Append(c);
            }

            return text.Append('"').ToString();
        }

        static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            value = value.Replace("\n", " ");
            return value.IndexOfAny(new[] { ',', '"' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
