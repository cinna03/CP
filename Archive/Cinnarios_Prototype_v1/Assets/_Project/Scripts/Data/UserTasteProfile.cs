using System;
using System.Collections.Generic;
using Capstone.App.Services;
using UnityEngine;

namespace Capstone.App.Data
{
    /// <summary>
    /// What this visit has learned about the buyer.
    /// The open visit stays in memory. LocalProfileStore keeps the history on the device.
    /// Empty text means that fact is still unknown.
    /// </summary>
    public sealed class UserTasteProfile
    {
        public const string LivingRoom = "Living Room";
        public const string Bedroom = "Bedroom";
        public const string Hallway = "Hallway";
        public const string Workspace = "Workspace";
        public const string BrightLight = "Bright Natural Light";
        public const string WarmLight = "Warm Lamp Light";
        public const string SoftLight = "Soft / Neutral";
        public const string AboveSofa = "Above Sofa";
        public const string AboveBed = "Above Bed";
        public const string FocalWall = "Focal Wall";
        public const string DeskShelf = "Desk / Shelf";

        readonly Dictionary<string, float> styles = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, float> moods = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, float> colours = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public string TargetRoom = "";
        public string WallLighting = "";
        public string FurnitureContext = "";

        public IReadOnlyDictionary<string, float> LikedStyles => styles;
        public IReadOnlyDictionary<string, float> LikedMoods => moods;
        public IReadOnlyDictionary<string, float> LikedColours => colours;

        public void NoteDecision(string style, string mood, string colour, float change)
        {
            Add(styles, style, change);
            Add(moods, mood, change);
            Add(colours, colour, change);
            if (Application.isPlaying || ArtworkSessionState.PersistOutsidePlay)
                LocalProfileStore.AddTaste(new UserProfileTracker().CurrentEntranceCode, style, mood, colour, change);
        }

        public float Weight(IReadOnlyDictionary<string, float> weights, string tag)
        {
            if (weights == null || string.IsNullOrWhiteSpace(tag))
                return 0f;
            float value;
            return weights.TryGetValue(tag.Trim(), out value) ? value : 0f;
        }

        /// <summary>Sets a field only when the buyer has not already answered it.</summary>
        public bool Remember(string room, string lighting, string furniture)
        {
            bool changed = false;
            if (TargetRoom.Length == 0 && !string.IsNullOrWhiteSpace(room))
            {
                TargetRoom = room.Trim();
                changed = true;
            }

            if (WallLighting.Length == 0 && !string.IsNullOrWhiteSpace(lighting))
            {
                WallLighting = lighting.Trim();
                changed = true;
            }

            if (FurnitureContext.Length == 0 && !string.IsNullOrWhiteSpace(furniture))
            {
                FurnitureContext = furniture.Trim();
                changed = true;
            }

            if (changed)
                StorePlace();
            return changed;
        }

        void StorePlace()
        {
            if (!Application.isPlaying && !ArtworkSessionState.PersistOutsidePlay)
                return;
            LocalProfileStore.RememberPlace(new UserProfileTracker().CurrentEntranceCode, TargetRoom, WallLighting, FurnitureContext);
        }

        /// <summary>Replaces the open visit with the copy stored for this entrance code.</summary>
        public void ApplyVisit(string room, string lighting, string furniture, IList<StoredWeight> styles, IList<StoredWeight> moods, IList<StoredWeight> colours)
        {
            bool any = !string.IsNullOrEmpty(room) || !string.IsNullOrEmpty(lighting) || !string.IsNullOrEmpty(furniture)
                || HasWeight(styles) || HasWeight(moods) || HasWeight(colours);
            if (!any)
                return;

            Clear();
            TargetRoom = room ?? "";
            WallLighting = lighting ?? "";
            FurnitureContext = furniture ?? "";
            Set(this.styles, styles);
            Set(this.moods, moods);
            Set(this.colours, colours);
        }

        /// <summary>Accepts one of the quick replies. Exact labels only.</summary>
        public bool Accept(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return false;

            string value = answer.Trim();
            if (TargetRoom.Length == 0 && (value == LivingRoom || value == Bedroom || value == Hallway || value == Workspace))
            {
                TargetRoom = value;
                StorePlace();
                return true;
            }

            if (WallLighting.Length == 0 && (value == BrightLight || value == WarmLight || value == SoftLight))
            {
                WallLighting = value;
                StorePlace();
                return true;
            }

            if (FurnitureContext.Length == 0 && (value == AboveSofa || value == AboveBed || value == FocalWall || value == DeskShelf))
            {
                FurnitureContext = value;
                StorePlace();
                return true;
            }

            return false;
        }

        public PreferencePrompt GetNextMissingPreferenceQuestion()
        {
            if (TargetRoom.Length == 0)
            {
                return new PreferencePrompt
                {
                    Question = "Which room is this wall in?",
                    Options = new[] { LivingRoom, Bedroom, Hallway, Workspace }
                };
            }

            if (WallLighting.Length == 0)
            {
                return new PreferencePrompt
                {
                    Question = "What light falls on this wall?",
                    Options = new[] { BrightLight, WarmLight, SoftLight }
                };
            }

            if (FurnitureContext.Length == 0)
            {
                return new PreferencePrompt
                {
                    Question = "Where will the work sit?",
                    Options = new[] { AboveSofa, AboveBed, FocalWall, DeskShelf }
                };
            }

            return new PreferencePrompt();
        }

        /// <summary>Forgets the room, light, furniture, and tag weights learned in this visit.</summary>
        public void Clear()
        {
            TargetRoom = "";
            WallLighting = "";
            FurnitureContext = "";
            styles.Clear();
            moods.Clear();
            colours.Clear();
        }

        static bool HasWeight(IList<StoredWeight> weights)
        {
            if (weights == null)
                return false;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] != null && !string.IsNullOrWhiteSpace(weights[i].Tag))
                    return true;
            }

            return false;
        }

        static void Set(Dictionary<string, float> target, IList<StoredWeight> weights)
        {
            if (target == null || weights == null)
                return;
            for (int i = 0; i < weights.Count; i++)
            {
                StoredWeight note = weights[i];
                if (note == null || string.IsNullOrWhiteSpace(note.Tag))
                    continue;
                target[note.Tag.Trim()] = note.Weight;
            }
        }

        static void Add(Dictionary<string, float> weights, string tag, float change)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            float current;
            weights.TryGetValue(tag.Trim(), out current);
            weights[tag.Trim()] = current + change;
        }
    }

    /// <summary>The next fact the visit still needs, with the replies the buyer can tap.</summary>
    public sealed class PreferencePrompt
    {
        public string Question = "";
        public string[] Options = new string[0];

        public bool HasQuestion
        {
            get { return !string.IsNullOrWhiteSpace(Question) && Options != null && Options.Length > 0; }
        }
    }
}
