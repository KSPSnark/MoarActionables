using KSP.Localization;
using System.Collections.Generic;

namespace MoarActionables
{
    /// <summary>
    /// Exposes SAS to action groups. One instance of this module will expose one SAS mode
    /// as an action in the action group menu. If you want to include multiple modes, then
    /// include multiple instances of ModuleSasActions.
    /// </summary>
    public class ModuleSasActions : PartModule, IComparer<ModuleSasActions>
    {
        // Localization tags for UI stuff.
        private const string TAG_SAS_TOGGLE        = "#autoLOC_6001371";  // "SAS"
        private const string TAG_SAS_ACTION_FORMAT = "#autoLOC_8004190"; // "<<1>>: <<2>>" (also considered #autoLOC_8002171, "<<1>> (<<2>>)"

        // Also considered #autoLOC_900582 ("Toggle SAS") as more appropriate, but there's very
        // limited horizontal screen real estate in the action group UI, so it's best to keep
        // the GUI screen names as short as possible.

        private static readonly Dictionary<VesselAutopilot.AutopilotMode, string> actionGuiNames = new Dictionary<VesselAutopilot.AutopilotMode, string>();

        /// <summary>
        /// The SAS mode that this module will set.
        /// </summary>
        [KSPField]
        public VesselAutopilot.AutopilotMode sasMode = VesselAutopilot.AutopilotMode.StabilityAssist;

        /// <summary>
        /// The main action of this module.
        /// </summary>
        /// <param name="actionParam"></param>
        [KSPAction(TAG_SAS_TOGGLE)]
        public void ToggleSas(KSPActionParam actionParam)
        {
            if (!AutopilotEnabled) return;

            SasActionsVesselModule vesselModule = SasActionsVesselModule.Current;
            if (vesselModule == null) return;
            if (vesselModule.resolver.HasConflict(this, actionParam.group)) return;

            EnableAutopilot(sasMode);
        }
        private BaseAction ToggleAction { get { return Actions["ToggleSas"]; } }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ToggleAction.guiName = GetActionUiDescription(sasMode);
        }

        /// <summary>
        /// Gets the autopilot for the current vessel.
        /// </summary>
        private VesselAutopilot Autopilot
        {
            get
            {
                return (vessel == null) ? null : vessel.Autopilot;
            }
        }

        /// <summary>
        /// Gets whether SAS is currently enabled.
        /// </summary>
        private bool AutopilotEnabled
        {
            get
            {
                return (Autopilot == null) ? false : Autopilot.Enabled;
            }
        }

        /// <summary>
        /// Enables autopilot in the specified mode.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private bool EnableAutopilot(VesselAutopilot.AutopilotMode mode)
        {
            bool success = (Autopilot == null) ? false : Autopilot.Enable(mode.RemappedToAccountForBizarreKspBug());
            if (success)
            {
                Logging.Log(part.LoggableName() + " set SAS mode to " + mode);
            }
            else
            {
                Logging.Log(part.LoggableName() + " can't set SAS mode to " + mode + " (may not be available for this vessel)");
            }
            return success;
        }

        /// <summary>
        /// Compares two ModuleSasActions, for purposes of conflict resolution
        /// </summary>
        /// <param name="module1"></param>
        /// <param name="module2"></param>
        /// <returns></returns>
        public int Compare(ModuleSasActions module1, ModuleSasActions module2)
        {
            // Prefer modules that are closer to the root part of the vessel.
            int stepsComparison = module2.part.StepsFromRoot().CompareTo(module1.part.StepsFromRoot());
            if (stepsComparison != 0) return stepsComparison;

            // Prefer modules that have the lowest SAS mode.
            return module2.sasMode.CompareTo(module1.sasMode);
        }

        /// <summary>
        /// Get a UI string for describing the action for a given SAS mode.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private static string GetActionUiDescription(VesselAutopilot.AutopilotMode mode)
        {
            string description = null;
            if (!actionGuiNames.TryGetValue(mode, out description))
            {
                description = Localizer.Format(
                    TAG_SAS_ACTION_FORMAT,
                    TAG_SAS_TOGGLE,
                    mode.LocalizationTag());
                actionGuiNames.Add(mode, description);
            }
            return description;
        }

        public class SasActionsVesselModule : VesselModule
        {
            internal ActionGroupConflictResolver<ModuleSasActions> resolver = new ActionGroupConflictResolver<ModuleSasActions>();

            public static SasActionsVesselModule Current
            {
                get
                {
                    Vessel vessel = FlightGlobals.ready ? FlightGlobals.ActiveVessel : null;
                    if (vessel == null) return null;
                    for (int i = 0; i < vessel.vesselModules.Count; ++i)
                    {
                        SasActionsVesselModule resolver = vessel.vesselModules[i] as SasActionsVesselModule;
                        if (resolver != null) return resolver;
                    }
                    return null;
                }
            }
        }
    }
}
