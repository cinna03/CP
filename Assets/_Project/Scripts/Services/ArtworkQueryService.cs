using System;
using System.Collections.Generic;
using Capstone.App.Data;

namespace Capstone.App.Services
{
    /// <summary>
    /// The written reply. Size and colour are filled from the chosen size and the stored colour.
    /// English, French, and Kinyarwanda use the same word lists as the visit table.
    /// An empty story note stays empty, and the reply says so.
    /// </summary>
    [Serializable]
    public sealed class ArtworkQueryService : IArtworkQueryService
    {
        public const string FixedLine = "I can only answer size, colour, or the story note from the written table.";
        public const string EmptyStory = "No story note is stored for this artwork.";
        public const string CuratorLabel = "Curator's note: ";
        public const string NoArtistNote = "A note from the artist has not been added yet.";
        public const string NoInspiration = "The artist's inspiration has not been recorded yet.";
        public const string EmptyColour = "No colour is stored.";
        public const string SizeRedirect = "Check if the size fits";
        public const string VibeRedirect = "Match my room's vibe";
        public const string ColourRedirect = "See how the colours work";
        public const string PivotLine = "That really comes down to what you're going for in your space—whether you want a bold focal point, a calm atmosphere, or an easy colour match. What matters most to you for this wall?";

        public string DescribeSize(ArtworkData work, int sizeIndex)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return FixedLine;

            string wide = ArtworkData.Centimetres(size.x);
            string tall = ArtworkData.Centimetres(size.y);
            string metresWide = ArtworkData.Centimetres(size.x / 100f);
            string metresTall = ArtworkData.Centimetres(size.y / 100f);
            return "This work is " + wide + " cm wide and " + tall + " cm tall. On the wall that is " + metresWide + " m by " + metresTall + " m.";
        }

        public string DescribeColour(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.Colour))
                return EmptyColour;

            return "The stored colour is " + work.Colour.Trim() + ".";
        }

        public string DescribeStory(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (!string.IsNullOrWhiteSpace(work.CuratorNote))
                return CuratorLabel + work.CuratorNote.Trim();
            if (string.IsNullOrWhiteSpace(work.StoryNote))
                return EmptyStory;

            return work.StoryNote;
        }

        /// <summary>
        /// What inspired the work. Only the artist can say this, so an empty field is reported as empty
        /// and the curator's interpretation is offered instead, clearly labelled.
        /// </summary>
        public string DescribeInspiration(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (!string.IsNullOrWhiteSpace(work.InspirationNote))
                return work.InspirationNote.Trim();
            if (!string.IsNullOrWhiteSpace(work.CuratorNote))
                return NoInspiration + " " + CuratorLabel + work.CuratorNote.Trim();
            return NoInspiration;
        }

        public string DescribeFit(ArtworkData work, int sizeIndex)
        {
            string size = DescribeSize(work, sizeIndex);
            int prints = work.AvailableSizes != null ? work.AvailableSizes.Count : 0;
            if (prints <= 1)
                return size + " This is the only print stored, so the same size is what would sit on a desk or a sofa wall.";

            return size + " " + prints + " prints are stored. The smaller one suits a desk. A sofa wall can take the larger one.";
        }

        public string DescribePlacement(ArtworkData work, int sizeIndex)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string size = DescribeSize(work, sizeIndex);
            Orientation facing = work.OrientationAt(sizeIndex);
            SizeTier tier = work.SizeTierAt(sizeIndex);
            string spot = Place(facing, tier);
            return size + " " + spot + " Hang the centre near 145 cm from the floor, about eye level.";
        }

        public string DescribeTaste(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string mood = string.IsNullOrWhiteSpace(work.Mood) ? "" : work.Mood.Trim();
            string style = string.IsNullOrWhiteSpace(work.Style) ? "" : work.Style.Trim();
            string colour = string.IsNullOrWhiteSpace(work.Colour) ? "" : work.Colour.Trim();
            string moodLine = TasteOfMood(mood, style);
            string colourLine = TasteOfColour(colour);
            if (moodLine.Length == 0 && colourLine.Length == 0)
                return "No mood, style, or colour is stored, so a plain light wall is the safest pairing.";
            if (colourLine.Length == 0)
                return moodLine;
            if (moodLine.Length == 0)
                return colourLine;
            return moodLine + " " + colourLine;
        }

        static string Place(Orientation facing, SizeTier tier)
        {
            if (tier == SizeTier.Large && facing == Orientation.Landscape)
                return "A large landscape suits the wall above a sofa or a bed, and it can be the focal wall.";
            if (tier == SizeTier.Large && facing == Orientation.Portrait)
                return "A large portrait suits a focal wall, with clear space on either side.";
            if (tier == SizeTier.Large)
                return "A large square suits a focal wall.";
            if (tier == SizeTier.Medium && facing == Orientation.Landscape)
                return "A medium landscape suits the wall above a sofa or a bed.";
            if (tier == SizeTier.Small && facing == Orientation.Portrait)
                return "A small portrait suits a narrow hallway, a pillar, or a small group of works.";
            if (facing == Orientation.Landscape)
                return "This landscape needs a wide enough wall. Above a sofa is the usual place.";
            if (facing == Orientation.Portrait)
                return "This portrait suits a narrower wall, or a place beside another work.";
            return "This square suits a calm wall with equal space around it.";
        }

        static string TasteOfMood(string mood, string style)
        {
            string token = (mood + " " + style).ToLowerInvariant();
            if (token.Contains("calm") || token.Contains("quiet") || token.Contains("still") || token.Contains("soft"))
                return "A calm work wants a pale wall, warm low light, and plain light wood.";
            if (token.Contains("bold") || token.Contains("bright") || token.Contains("direct") || token.Contains("graphic"))
                return "A bold work wants a quiet wall and simple furniture, so the picture stays the strongest colour in the room.";
            if (mood.Length == 0 && style.Length == 0)
                return "";
            return "The " + (mood.Length > 0 ? mood.ToLowerInvariant() + " mood" : style.ToLowerInvariant() + " style") + " sits best on a plain wall with one calm piece of furniture nearby.";
        }

        static string TasteOfColour(string colour)
        {
            if (colour.Length == 0)
                return "";

            switch (colour.ToLowerInvariant())
            {
                case "blue":
                    return "Blue pairs with a cream wall, warm wood, and a lamp rather than a cool overhead light.";
                case "pink":
                    return "Pink pairs with a white or warm grey wall and pale upholstery.";
                case "ochre":
                    return "Ochre pairs with linen, a warm bulb, and wood rather than black metal.";
                case "green":
                    return "Green pairs with a pale wall and a little wood or a plant nearby.";
                case "red":
                    return "Red pairs with a neutral wall and furniture that does not add another strong colour.";
                case "orange":
                    return "Orange pairs with a white wall and a simple seat in grey, cream, or wood.";
                case "grey":
                    return "Grey pairs with warmer wood and a soft lamp so the room does not go cold.";
                case "white":
                    return "White pairs with oak or stone and needs even light so the edges stay visible.";
                default:
                    return "The " + colour.ToLowerInvariant() + " wants a wall close to white, so the stored colour stays readable.";
            }
        }

        public string DescribeOrientation(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            Orientation facing = work.Orientation;
            if (facing == Orientation.Landscape)
                return "This work is landscape. It is wider than it is tall.";
            if (facing == Orientation.Portrait)
                return "This work is portrait. It is taller than it is wide.";
            return "This work is square.";
        }

        public string DescribeMedium(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.Medium))
                return "No medium is stored.";
            return "The stored medium is " + work.Medium.Trim() + ".";
        }

        public string DescribeArtist(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            string note = string.IsNullOrWhiteSpace(work.ArtistNote) ? NoArtistNote : work.ArtistNote.Trim();
            if (string.IsNullOrWhiteSpace(work.Artist))
                return "No artist is stored. " + note;
            return "The stored artist is " + work.Artist.Trim() + ". " + note;
        }

        public string DescribeYear(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (work.Year <= 0)
                return "No year is stored.";
            return "The stored year is " + work.Year + ".";
        }

        public string DescribeMood(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.Mood))
                return "No mood is stored.";
            return "The stored mood is " + work.Mood.Trim() + ".";
        }

        public string DescribeStyle(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.Style))
                return "No style is stored.";
            return "The stored style is " + work.Style.Trim() + ".";
        }

        public string DescribeTitle(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.Title))
                return "No title is stored.";
            return "The title is " + work.Title.Trim() + ".";
        }

        public string DescribePrice(ArtworkData work)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));
            if (string.IsNullOrWhiteSpace(work.PriceBand))
                return "No price band is stored.";
            return "The stored price band is " + work.PriceBand.Trim() + ".";
        }

        public ConsultReply Consult(ArtworkData work, int sizeIndex, FrameOption frame, IReadOnlyList<ArtworkData> catalogue, ArtworkSessionState visit, string question)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            UserTasteProfile taste = visit != null ? visit.Taste : null;
            IReadOnlyList<TriedPiece> tried = visit != null ? visit.Tried : null;
            string asked = question ?? "";
            if (IsComparison(asked))
            {
                return new ConsultReply
                {
                    Text = CompareTried(work, tried, taste, asked),
                    Options = new string[0]
                };
            }

            Learn(taste, asked);
            ConsultReply recall = Recall(visit, asked);
            if (recall != null)
                return recall;

            ConsultReply sizes = SizeObjection(work, sizeIndex, catalogue, asked);
            if (sizes != null)
                return sizes;

            ConsultReply found = CatalogueAsk(work, catalogue, asked);
            if (found != null)
                return found;

            string concern = Concern(asked);
            string body;
            if (concern == "size")
                body = DescribeProportion(work, sizeIndex, taste);
            else if (concern == "vibe")
                body = DescribeCraft(work, taste);
            else if (concern == "colour")
                body = DescribeLighting(work, frame, taste);
            else if (IsChoice(asked))
                body = ChoiceReply(work, sizeIndex, frame, taste, asked);
            else if (Contains(asked, "fit & size", "proportion", "viewing", "distance", "ratio"))
                body = DescribeProportion(work, sizeIndex, taste);
            else if (Contains(asked, "lighting & colour", "lighting", "glare", "sunny", "window", "lamp"))
                body = DescribeLighting(work, frame, taste);
            else if (Contains(asked, "story & craft", "craft", "longevity", "medium"))
                body = DescribeCraft(work, taste);
            else if (Contains(asked, "where to hang", "where should", "hang", "place"))
                body = DescribePlacement(work, sizeIndex);
            else
                body = Answer(work, sizeIndex, asked);

            if (body == FixedLine)
            {
                return new ConsultReply
                {
                    Text = PivotLine,
                    Options = new[] { SizeRedirect, VibeRedirect, ColourRedirect }
                };
            }

            return WithNextPreference(body, taste);
        }

        static ConsultReply WithNextPreference(string body, UserTasteProfile taste)
        {
            if (taste == null)
                return new ConsultReply { Text = body, Options = new string[0] };

            PreferencePrompt next = taste.GetNextMissingPreferenceQuestion();
            if (!next.HasQuestion)
                return new ConsultReply { Text = body, Options = new string[0] };

            return new ConsultReply
            {
                Text = body + "\n\n" + next.Question,
                Options = next.Options
            };
        }

        static string Concern(string asked)
        {
            string text = (asked ?? "").Trim();
            if (text.Equals(SizeRedirect, StringComparison.OrdinalIgnoreCase))
                return "size";
            if (text.Equals(VibeRedirect, StringComparison.OrdinalIgnoreCase))
                return "vibe";
            if (text.Equals(ColourRedirect, StringComparison.OrdinalIgnoreCase))
                return "colour";

            if (Contains(asked, "worried about size", "does it fit", "will it fit", "size fit", "check if the size"))
                return "size";
            if (Contains(asked, "colours work", "colors work", "colour match", "color match", "colour harmony", "color harmony", "how the colours", "how the colors", "clash"))
                return "colour";
            if (Contains(asked, "something calm", "something bold", "something quiet", "room's vibe", "atmosphere", "vibe", "calm", "cosy", "cozy"))
                return "vibe";
            return "";
        }

        static ConsultReply Recall(ArtworkSessionState visit, string asked)
        {
            bool remind = Contains(asked, "remind");
            bool tried = remind || Contains(asked, "tried", "names");
            bool saved = remind || Contains(asked, "saved", "favorites", "favourites", "favorite", "favourite");
            if (!tried && !saved)
                return null;

            var actions = new List<ConsultAction>();
            string text = "";
            if (tried)
                text = ListTried(visit, actions);
            if (saved)
            {
                if (text.Length > 0)
                    text += "\n\n";
                text += ListSaved(visit, actions);
            }

            return new ConsultReply { Text = text, Actions = actions.ToArray() };
        }

        static string ListTried(ArtworkSessionState visit, List<ConsultAction> actions)
        {
            int count = 0;
            string text = "Works tried this visit:";
            if (visit != null && visit.Tried != null)
            {
                for (int i = 0; i < visit.Tried.Count; i++)
                {
                    TriedPiece piece = visit.Tried[i];
                    if (piece == null || piece.Work == null)
                        continue;
                    count++;
                    text += "\n" + piece.Work.Title + ", " + piece.Work.FormatSizeLabel(piece.SizeIndex) + ".";
                    AddPut(actions, piece.Work, piece.SizeIndex);
                }
            }

            if (count == 0)
                return "No works have been tried in this visit yet.";
            return text;
        }

        static string ListSaved(ArtworkSessionState visit, List<ConsultAction> actions)
        {
            int count = 0;
            string text = "Saved works:";
            if (visit != null && visit.Shortlist != null)
            {
                for (int i = 0; i < visit.Shortlist.Count; i++)
                {
                    ArtworkData work = visit.Shortlist[i];
                    if (work == null)
                        continue;
                    count++;
                    text += "\n" + work.Title + ", " + work.FormatSizeLabel(0) + ".";
                    AddPut(actions, work, 0);
                }
            }

            if (count == 0)
                return "No works have been saved yet.";
            return text;
        }

        static void AddPut(List<ConsultAction> actions, ArtworkData work, int sizeIndex)
        {
            if (work == null)
                return;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].Work != null && actions[i].Work.Identifier == work.Identifier)
                    return;
            }

            actions.Add(new ConsultAction
            {
                Label = "Put " + work.Title + " on wall",
                Work = work,
                SizeIndex = sizeIndex < 0 ? 0 : sizeIndex
            });
        }

        static ConsultReply SizeObjection(ArtworkData work, int sizeIndex, IReadOnlyList<ArtworkData> catalogue, string asked)
        {
            bool smaller = Contains(asked, "too big", "smaller", "won't fit", "wont fit", "will not fit", "does not fit", "doesn't fit");
            bool larger = Contains(asked, "too small", "larger");
            bool different = Contains(asked, "different size");
            if (!smaller && !larger && !different)
                return null;

            float currentArea = Area(work, sizeIndex);
            var indexes = new List<int>();
            int count = work.AvailableSizes != null ? work.AvailableSizes.Count : 0;
            for (int i = 0; i < count; i++)
            {
                if (i == sizeIndex)
                    continue;
                float area = Area(work, i);
                if (area <= 0f)
                    continue;
                if (different || (smaller && larger) || (smaller && area < currentArea) || (larger && area > currentArea))
                    indexes.Add(i);
            }

            if (indexes.Count > 0)
            {
                var actions = new ConsultAction[indexes.Count];
                for (int i = 0; i < indexes.Count; i++)
                {
                    actions[i] = new ConsultAction
                    {
                        Label = "Switch to " + SizeWords(work, indexes[i]),
                        SizeIndex = indexes[i]
                    };
                }

                return new ConsultReply
                {
                    Text = "You don't need to give up on this piece—it also comes in " + JoinSizes(work, indexes) + ".",
                    Actions = actions
                };
            }

            bool wantSmaller = smaller && !larger;
            bool wantLarger = larger && !smaller;
            var others = new List<ArtworkData>();
            if (catalogue != null)
            {
                for (int i = 0; i < catalogue.Count; i++)
                {
                    ArtworkData other = catalogue[i];
                    if (other == null || other.Identifier == work.Identifier)
                        continue;
                    float area = Area(other, 0);
                    if (area <= 0f)
                        continue;
                    if (wantSmaller && area >= currentArea)
                        continue;
                    if (wantLarger && area <= currentArea)
                        continue;
                    others.Add(other);
                }
            }

            others.Sort(delegate (ArtworkData a, ArtworkData b)
            {
                float left = Area(a, 0);
                float right = Area(b, 0);
                if (wantLarger)
                    return left.CompareTo(right);
                return right.CompareTo(left);
            });

            if (others.Count == 0)
                return new ConsultReply { Text = "This is the only print stored for this piece, and no other work in the catalogue is a clearer size." };

            int take = others.Count < 4 ? others.Count : 4;
            var suggestions = new ConsultAction[take];
            string text = "This piece has no " + (wantSmaller ? "smaller" : wantLarger ? "larger" : "other") + " print. These other works are a different size:";
            for (int i = 0; i < take; i++)
            {
                text += "\n" + others[i].Title + ", " + others[i].FormatSizeLabel(0) + ".";
                suggestions[i] = new ConsultAction
                {
                    Label = "Try " + others[i].Title,
                    Work = others[i],
                    SizeIndex = 0
                };
            }

            return new ConsultReply { Text = text, Actions = suggestions };
        }

        static ConsultReply CatalogueAsk(ArtworkData current, IReadOnlyList<ArtworkData> catalogue, string asked)
        {
            if (!Contains(asked, "catalogue", "catalog", "what else", "do you have", "show me", "any other", "landscapes", "portraits"))
                return null;

            if (catalogue == null || catalogue.Count == 0)
                return new ConsultReply { Text = "The catalogue is not loaded." };

            string[] tokens = SearchTokens(asked);
            var matches = new List<ArtworkData>();
            for (int i = 0; i < catalogue.Count; i++)
            {
                ArtworkData work = catalogue[i];
                if (work == null)
                    continue;
                if (tokens.Length == 0 && current != null && work.Identifier == current.Identifier)
                    continue;
                if (tokens.Length > 0 && !MatchesAll(work, tokens))
                    continue;
                matches.Add(work);
                if (matches.Count == 5)
                    break;
            }

            if (matches.Count == 0)
                return new ConsultReply { Text = "None of the stored works match that." };

            var actions = new ConsultAction[matches.Count];
            string text = tokens.Length == 0 ? "Other works in the catalogue:" : "These works match:";
            for (int i = 0; i < matches.Count; i++)
            {
                text += "\n" + matches[i].Title + ", " + matches[i].FormatSizeLabel(0) + ", " + matches[i].Colour + ".";
                actions[i] = new ConsultAction
                {
                    Label = "Try " + matches[i].Title,
                    Work = matches[i],
                    SizeIndex = 0
                };
            }

            return new ConsultReply { Text = text, Actions = actions };
        }

        static string[] SearchTokens(string asked)
        {
            string[] parts = (asked ?? "").ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r', '?', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
            var tokens = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i];
                if (token.EndsWith("s") && token.Length > 4)
                    token = token.Substring(0, token.Length - 1);
                if (IsStop(token))
                    continue;
                tokens.Add(token);
            }

            return tokens.ToArray();
        }

        static bool IsStop(string token)
        {
            return token == "do" || token == "you" || token == "have" || token == "any" || token == "show" || token == "me"
                || token == "piece" || token == "work" || token == "the" || token == "a" || token == "an" || token == "in"
                || token == "catalogue" || token == "catalog" || token == "what" || token == "else" || token == "is"
                || token == "there" || token == "some" || token == "of" || token == "please" || token == "other"
                || token == "another" || token == "painting" || token == "artwork" || token == "for" || token == "my"
                || token == "your" || token == "with" || token == "and" || token == "or" || token == "to" || token == "from";
        }

        static bool MatchesAll(ArtworkData work, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!Matches(work, tokens[i]))
                    return false;
            }

            return true;
        }

        static bool Matches(ArtworkData work, string token)
        {
            if (token == "landscape" || token == "portrait" || token == "square")
                return string.Equals(work.Orientation.ToString(), token, StringComparison.OrdinalIgnoreCase);
            if (token == "small" || token == "medium" || token == "large")
                return string.Equals(work.SizeTier.ToString(), token, StringComparison.OrdinalIgnoreCase);
            if (SameText(work.Colour, token) || SameText(work.Mood, token) || SameText(work.Style, token) || SameText(work.Medium, token))
                return true;
            if (!string.IsNullOrEmpty(work.Title) && work.Title.ToLowerInvariant().Contains(token))
                return true;
            if (!string.IsNullOrEmpty(work.Artist) && work.Artist.ToLowerInvariant().Contains(token))
                return true;
            return false;
        }

        static bool SameText(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value) && string.Equals(value.Trim(), token, StringComparison.OrdinalIgnoreCase);
        }

        static float Area(ArtworkData work, int sizeIndex)
        {
            if (work == null || !work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return 0f;
            return size.x * size.y;
        }

        static string SizeWords(ArtworkData work, int sizeIndex)
        {
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return work.FormatSizeLabel(sizeIndex);
            return ArtworkData.Centimetres(size.x) + " × " + ArtworkData.Centimetres(size.y) + " cm";
        }

        static string JoinSizes(ArtworkData work, List<int> indexes)
        {
            string text = "";
            for (int i = 0; i < indexes.Count; i++)
            {
                if (i > 0 && i == indexes.Count - 1)
                    text += " and ";
                else if (i > 0)
                    text += ", ";
                text += SizeWords(work, indexes[i]);
            }

            return text;
        }

        public string DescribeProportion(ArtworkData work, int sizeIndex, UserTasteProfile taste)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string distance;
            SizeTier tier = work.SizeTierAt(sizeIndex);
            if (tier == SizeTier.Small)
                distance = "about 1 metre";
            else if (tier == SizeTier.Medium)
                distance = "about 1.5 to 2 metres";
            else
                distance = "about 2.5 to 3 metres";

            string ratio = "Above a sofa or a bed, keep the work near two thirds the width of that furniture.";
            if (taste != null && taste.FurnitureContext == UserTasteProfile.DeskShelf)
                ratio = "On a desk or shelf, leave a clear margin around the frame.";
            else if (taste != null && taste.FurnitureContext == UserTasteProfile.FocalWall)
                ratio = "On a focal wall, leave open wall on both sides. The work should not fill the whole width.";
            else if (taste != null && taste.FurnitureContext == UserTasteProfile.AboveSofa)
                ratio = "Above the sofa, keep the work near two thirds the sofa's width.";
            else if (taste != null && taste.FurnitureContext == UserTasteProfile.AboveBed)
                ratio = "Above the bed, keep the work near two thirds the bed's width.";

            return DescribeSize(work, sizeIndex) + " Step back " + distance + ". " + ratio;
        }

        public string DescribeLighting(ArtworkData work, FrameOption frame, UserTasteProfile taste)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string colour = string.IsNullOrWhiteSpace(work.Colour) ? "the stored colour" : work.Colour.Trim();
            string frameName = frame != null && !string.IsNullOrWhiteSpace(frame.FrameName) ? frame.FrameName.Trim() : "Unframed";
            string light = taste != null ? taste.WallLighting : "";
            string glare;
            if (light == UserTasteProfile.BrightLight)
                glare = "Bright natural light near a window can glare. A glass or dark frame shows that more. " + frameName + " will read more quietly if the work sits off the direct window.";
            else if (light == UserTasteProfile.WarmLight)
                glare = "Warm lamp light deepens " + colour + " and softens glare. " + frameName + " will pick up that warmth along the edge.";
            else if (light == UserTasteProfile.SoftLight)
                glare = "Soft neutral light lets " + colour + " read as stored. " + frameName + " will not throw a hard reflection.";
            else
                glare = "If the wall faces a window, keep the work out of the direct sun so " + colour + " does not glare. A lamp gives " + frameName + " a softer edge.";

            return DescribeColour(work) + " " + glare;
        }

        public string DescribeCraft(ArtworkData work, UserTasteProfile taste)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string room = taste != null && taste.TargetRoom.Length > 0 ? taste.TargetRoom : "the room";
            string medium = string.IsNullOrWhiteSpace(work.Medium) ? "The medium is not stored." : "The " + work.Medium.Trim() + " surface is what will age on the wall.";
            string mood = string.IsNullOrWhiteSpace(work.Mood) ? "" : " The " + work.Mood.Trim() + " mood is what " + room + " will live with day to day.";
            string story = string.IsNullOrWhiteSpace(work.StoryNote) ? "" : " " + work.StoryNote.Trim();
            string lasting;
            string token = (work.Medium ?? "").ToLowerInvariant();
            if (token.Contains("oil"))
                lasting = " Oil stays rich if the wall is not in full sun.";
            else if (token.Contains("water"))
                lasting = " Watercolour wants indirect light, or the colour fades.";
            else if (token.Contains("ink"))
                lasting = " Ink wants shade. A bright window will fade it.";
            else if (token.Contains("acrylic"))
                lasting = " Acrylic stays stable in ordinary room light.";
            else
                lasting = "";

            return medium + lasting + mood + story;
        }

        static bool Learn(UserTasteProfile taste, string message)
        {
            if (taste == null || string.IsNullOrWhiteSpace(message))
                return false;

            string text = message.ToLowerInvariant();
            string room = "";
            string light = "";
            string furniture = "";
            if (text.Contains("living room") || text.Contains("livingroom") || text.Contains("lounge"))
                room = UserTasteProfile.LivingRoom;
            else if (text.Contains("bedroom"))
                room = UserTasteProfile.Bedroom;
            else if (text.Contains("hallway") || text.Contains("corridor"))
                room = UserTasteProfile.Hallway;
            else if (text.Contains("workspace") || text.Contains("office"))
                room = UserTasteProfile.Workspace;

            if (text.Contains("sunny") || text.Contains("window") || text.Contains("bright natural") || text.Contains("glare"))
                light = UserTasteProfile.BrightLight;
            else if (text.Contains("lamp") || text.Contains("warm lamp"))
                light = UserTasteProfile.WarmLight;
            else if (text.Contains("dark") || text.Contains("soft") || text.Contains("neutral"))
                light = UserTasteProfile.SoftLight;

            if (text.Contains("sofa"))
                furniture = UserTasteProfile.AboveSofa;
            else if (text.Contains("focal"))
                furniture = UserTasteProfile.FocalWall;
            else if (text.Contains("desk") || text.Contains("shelf"))
                furniture = UserTasteProfile.DeskShelf;
            else if (text.Contains("bed") && !text.Contains("bedroom"))
                furniture = UserTasteProfile.AboveBed;

            if (room.Length == 0 && furniture == UserTasteProfile.AboveBed)
                room = UserTasteProfile.Bedroom;

            if (text.Contains("calm"))
                taste.NoteDecision("", "Calm", "", 1f);
            if (text.Contains("quiet"))
                taste.NoteDecision("Quiet", "", "", 1f);
            if (text.Contains("bold"))
                taste.NoteDecision("Bold", "", "", 1f);
            if (text.Contains("still"))
                taste.NoteDecision("", "Still", "", 1f);

            return taste.Remember(room, light, furniture);
        }

        static bool IsComparison(string asked)
        {
            return Contains(asked, "compare", "which is bigger", "which fits better", "difference");
        }

        public string CompareTried(ArtworkData current, IReadOnlyList<TriedPiece> tried, UserTasteProfile taste, string asked)
        {
            int count = 0;
            if (tried != null)
            {
                for (int i = 0; i < tried.Count; i++)
                {
                    if (tried[i] != null && tried[i].Work != null)
                        count++;
                }
            }

            if (count < 2)
                return "Try another work on the wall before comparing.";

            if (Contains(asked, "bigger"))
                return Largest(tried);
            if (Contains(asked, "fits"))
                return BestFit(tried, taste);
            return Differences(current, tried);
        }

        static string Largest(IReadOnlyList<TriedPiece> tried)
        {
            TriedPiece best = null;
            float area = -1f;
            for (int i = 0; i < tried.Count; i++)
            {
                TriedPiece piece = tried[i];
                if (piece == null || piece.Work == null)
                    continue;
                if (!piece.Work.TryGetSizeCentimetres(piece.SizeIndex, out UnityEngine.Vector2 size))
                    continue;
                float next = size.x * size.y;
                if (next > area)
                {
                    area = next;
                    best = piece;
                }
            }

            if (best == null)
                return "No stored size is available to compare.";
            return best.Work.Title + " is the largest of the works tried. It is " + best.Work.FormatSizeLabel(best.SizeIndex) + ".";
        }

        static string BestFit(IReadOnlyList<TriedPiece> tried, UserTasteProfile taste)
        {
            TriedPiece best = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < tried.Count; i++)
            {
                TriedPiece piece = tried[i];
                if (piece == null || piece.Work == null)
                    continue;
                int score = FitScore(piece, taste);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = piece;
                }
            }

            if (best == null)
                return "No tried work can be compared yet.";
            string room = taste != null && taste.TargetRoom.Length > 0 ? taste.TargetRoom : "this wall";
            return best.Work.Title + " fits " + room + " more closely. It is " + best.Work.FormatSizeLabel(best.SizeIndex) + ", " + best.Work.Colour + ", " + best.Work.Mood + ".";
        }

        static int FitScore(TriedPiece piece, UserTasteProfile taste)
        {
            int score = 0;
            SizeTier tier = piece.Work.SizeTierAt(piece.SizeIndex);
            Orientation facing = piece.Work.OrientationAt(piece.SizeIndex);
            if (taste == null)
                return tier == SizeTier.Medium ? 1 : 0;
            if (taste.TargetRoom == UserTasteProfile.Bedroom || taste.TargetRoom == UserTasteProfile.Hallway || taste.FurnitureContext == UserTasteProfile.DeskShelf)
                score += tier == SizeTier.Small ? 3 : tier == SizeTier.Medium ? 1 : -1;
            if (taste.FurnitureContext == UserTasteProfile.AboveSofa && facing == Orientation.Landscape)
                score += 3;
            if (taste.TargetRoom == UserTasteProfile.LivingRoom && tier != SizeTier.Small)
                score += 2;
            return score;
        }

        static string Differences(ArtworkData current, IReadOnlyList<TriedPiece> tried)
        {
            string text = "Works tried this visit:";
            for (int i = 0; i < tried.Count; i++)
            {
                TriedPiece piece = tried[i];
                if (piece == null || piece.Work == null)
                    continue;
                string mark = current != null && piece.Work.Identifier == current.Identifier ? " (on the wall)" : "";
                text += "\n" + piece.Work.Title + mark + ": " + piece.Work.FormatSizeLabel(piece.SizeIndex) + ", " + piece.Work.Colour + ", " + piece.Work.Mood + ", " + piece.Status + ".";
            }

            return text;
        }

        static bool IsChoice(string asked)
        {
            return asked == UserTasteProfile.LivingRoom
                || asked == UserTasteProfile.Bedroom
                || asked == UserTasteProfile.Hallway
                || asked == UserTasteProfile.Workspace
                || asked == UserTasteProfile.BrightLight
                || asked == UserTasteProfile.WarmLight
                || asked == UserTasteProfile.SoftLight
                || asked == UserTasteProfile.AboveSofa
                || asked == UserTasteProfile.AboveBed
                || asked == UserTasteProfile.FocalWall
                || asked == UserTasteProfile.DeskShelf;
        }

        string ChoiceReply(ArtworkData work, int sizeIndex, FrameOption frame, UserTasteProfile taste, string asked)
        {
            if (asked == UserTasteProfile.BrightLight || asked == UserTasteProfile.WarmLight || asked == UserTasteProfile.SoftLight)
                return "Noted. " + DescribeLighting(work, frame, taste);
            return "Noted. " + DescribePlacement(work, sizeIndex) + " " + DescribeProportion(work, sizeIndex, taste);
        }

        public string Answer(ArtworkData work, int sizeIndex, string question)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            string asked = question ?? "";
            if (Contains(asked, "taille", "largeur", "hauteur"))
                return FrenchSize(work, sizeIndex);
            if (Contains(asked, "couleur"))
                return FrenchColour(work);
            if (Contains(asked, "ingano", "ubugari", "uburebure"))
                return KinyarwandaSize(work, sizeIndex);
            if (Contains(asked, "ibara"))
                return KinyarwandaColour(work);
            if (Contains(asked, "where", "place", "hang", "room", "sofa", "bed", "wall", "space"))
                return DescribePlacement(work, sizeIndex);
            if (Contains(asked, "suit", "match", "taste", "decor", "vibe", "furniture", "look good", "go with"))
                return DescribeTaste(work);
            if (Contains(asked, "fit", "desk"))
                return DescribeFit(work, sizeIndex);
            if (Contains(asked, "orientation", "shape", "portrait", "landscape", "square"))
                return DescribeOrientation(work);
            if (Contains(asked, "medium", "paint", "oil", "acrylic", "watercolour", "watercolor", "ink"))
                return DescribeMedium(work);
            ReplyRule storyRule = StoryRules.Find(asked);
            if (storyRule is ArtistRule)
                return storyRule.Answer(work, this);
            if (Contains(asked, "year", "painted in", "made in"))
                return DescribeYear(work);
            if (Contains(asked, "mood"))
                return DescribeMood(work);
            if (Contains(asked, "style"))
                return DescribeStyle(work);
            if (Contains(asked, "title", "called"))
                return DescribeTitle(work);
            if (Contains(asked, "price"))
                return DescribePrice(work);
            if (Contains(asked, "size", "centimetre", "centimeter", "width", "height", "big"))
                return DescribeSize(work, sizeIndex);
            if (Contains(asked, "colour", "color"))
                return DescribeColour(work);
            if (storyRule != null)
                return storyRule.Answer(work, this);

            return FixedLine;
        }

        string FrenchSize(ArtworkData work, int sizeIndex)
        {
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return FixedLine;

            return "Cette oeuvre mesure " + ArtworkData.Centimetres(size.x) + " cm de largeur et " + ArtworkData.Centimetres(size.y) + " cm de hauteur.";
        }

        string FrenchColour(ArtworkData work)
        {
            if (string.IsNullOrWhiteSpace(work.Colour))
                return EmptyColour;

            return "La couleur enregistree est " + work.Colour.Trim() + ".";
        }

        string KinyarwandaSize(ArtworkData work, int sizeIndex)
        {
            if (!work.TryGetSizeCentimetres(sizeIndex, out UnityEngine.Vector2 size))
                return FixedLine;

            return "Iyi shusho ifite ubugari bwa " + ArtworkData.Centimetres(size.x) + " cm n'uburebure bwa " + ArtworkData.Centimetres(size.y) + " cm.";
        }

        string KinyarwandaColour(ArtworkData work)
        {
            if (string.IsNullOrWhiteSpace(work.Colour))
                return EmptyColour;

            return "Ibara ryanditswe ni " + work.Colour.Trim() + ".";
        }

        static bool Contains(string question, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (question.IndexOf(words[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
