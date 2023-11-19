namespace MoarActionables
{
    /// <summary>
    /// Various convenient extensions.
    /// </summary>
    internal static class Extensions
    {
        private static readonly string TAG_STABILITY_ASSIST = "#autoLOC_900603";
        private static readonly string TAG_PROGRADE         = "#autoLOC_900597";
        private static readonly string TAG_RETROGRADE       = "#autoLOC_900607";
        private static readonly string TAG_NORMAL           = "#autoLOC_900602";
        private static readonly string TAG_ANTINORMAL       = "#autoLOC_7001230";
        private static readonly string TAG_RADIAL_IN        = "#autoLOC_900598";
        private static readonly string TAG_RADIAL_OUT       = "#autoLOC_900581";
        private static readonly string TAG_TARGET           = "#autoLOC_900591";
        private static readonly string TAG_ANTITARGET       = "#autoLOC_900589";
        private static readonly string TAG_MANEUVER         = "#autoLOC_900587";

        /// <summary>
        /// Extension method to get the localization tag for a given SAS mode.
        /// </summary>
        /// <param name="sasMode"></param>
        /// <returns></returns>
        public static string LocalizationTag(this VesselAutopilot.AutopilotMode sasMode)
        {
            switch (sasMode)
            {
                case VesselAutopilot.AutopilotMode.StabilityAssist:
                    return TAG_STABILITY_ASSIST;
                case VesselAutopilot.AutopilotMode.Prograde:
                    return TAG_PROGRADE;
                case VesselAutopilot.AutopilotMode.Retrograde:
                    return TAG_RETROGRADE;
                case VesselAutopilot.AutopilotMode.Normal:
                    return TAG_NORMAL;
                case VesselAutopilot.AutopilotMode.Antinormal:
                    return TAG_ANTINORMAL;
                case VesselAutopilot.AutopilotMode.RadialIn:
                    return TAG_RADIAL_IN;
                case VesselAutopilot.AutopilotMode.RadialOut:
                    return TAG_RADIAL_OUT;
                case VesselAutopilot.AutopilotMode.Target:
                    return TAG_TARGET;
                case VesselAutopilot.AutopilotMode.AntiTarget:
                    return TAG_ANTITARGET;
                case VesselAutopilot.AutopilotMode.Maneuver:
                    return TAG_MANEUVER;
                default:
                    // should never happen, but just in case
                    return sasMode.ToString();
            }
        }

        /// <summary>
        /// KSP has a truly bizarre bug in which if you tell VesselAutopilot to set the mode
        /// to RadialIn, it actually sets it to RadialOut, and vice versa.  Every other mode
        /// works fine, it's just *those specific two* that are reversed.  Apparently, it's
        /// been this way since forever.  ¯\_(ツ)_/¯
        /// Reference: https://bugs.kerbalspaceprogram.com/issues/13199
        /// </summary>
        /// <param name="actualMode"></param>
        /// <returns></returns>
        public static VesselAutopilot.AutopilotMode RemappedToAccountForBizarreKspBug(this VesselAutopilot.AutopilotMode actualMode)
        {
            switch (actualMode)
            {
                case VesselAutopilot.AutopilotMode.RadialIn:
                    return VesselAutopilot.AutopilotMode.RadialOut;
                case VesselAutopilot.AutopilotMode.RadialOut:
                    return VesselAutopilot.AutopilotMode.RadialIn;
                default:
                    return actualMode;
            }
        }

        public static string LoggableName(this Part part)
        {
            return (part.partInfo == null) ? part.partName : part.partInfo.name;
        }

        /// <summary>
        /// Returns how many steps this part is from the root of the vessel.
        /// Returns 0 for the root part, 1 for its children, 2 for their children, etc.
        /// </summary>
        public static int StepsFromRoot(this Part part)
        {
            int steps = 0;
            Part currentPart = part;
            if (currentPart == null) return int.MaxValue;
            while (currentPart.parent != null)
            {
                ++steps;
                currentPart = currentPart.parent;
            }
            return steps;
        }

        /// <summary>
        /// Returns the index of this module within its parent's modules list.
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public static int Index(this PartModule module)
        {
            if (module.part == null) return int.MaxValue;
            for (int index = 0; index < module.part.Modules.Count; ++index)
            {
                PartModule candidate = module.part.Modules[index];
                if (object.ReferenceEquals(module, candidate)) return index;
            }
            // this should never happen, but...
            return int.MaxValue;
        }
    }
}
