namespace Capstone.App.AR
{
    /// <summary>
    /// First-time guidance on the wall, as three numbered steps. Once the buyer has locked a work, the
    /// steps are not shown again on this phone; afterwards only short status lines appear.
    /// </summary>
    public sealed class WallCoach
    {
        public const string Step1 = "1 of 3  ·  Move the phone slowly across a wall";
        public const string Step2 = "2 of 3  ·  Tap the wall to hang the work";
        public const string Step3 = "3 of 3  ·  Drag it into place, then tap Lock";
        public const string Locked = "Locked. Walk around it, or choose another work to hang beside it";

        public bool Learned { get; private set; }

        public WallCoach(bool learned)
        {
            Learned = learned;
        }

        /// <summary>The line for this moment, or null when nothing needs saying.</summary>
        public string Line(bool wallSeen, bool workOnWall, int lockedCount)
        {
            if (workOnWall)
                return Learned ? null : Step3;
            if (lockedCount > 0)
                return Locked;
            if (!wallSeen)
                return Learned ? "Move across a wall" : Step1;
            return Learned ? "Tap the wall to hang it" : Step2;
        }

        /// <summary>Call when the buyer locks a work for the first time.</summary>
        public void MarkLearned()
        {
            Learned = true;
        }
    }
}
