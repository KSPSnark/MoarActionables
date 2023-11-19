using System.Collections.Generic;

namespace MoarActionables
{
    /// <summary>
    /// Helper class for dealing with situations where the thing that we're triggering via
    /// action group is "global" to the whole vessel, rather than being associated only with
    /// the part it's on.  Such situations can complicate matters if there are multiple
    /// different parts that are all trying to tinker with the same thing, possibly
    /// in contradictory ways.  They end up arm-wrestling.  So what this class does is
    /// to provide a way that can efficiently and deterministically pick a "winner"
    /// when there are multiple conflicting modules, so that the action gets triggered
    /// only once.
    /// </summary>
    /// <typeparam name="Tmodule"></typeparam>
    internal class ActionGroupConflictResolver<Tmodule> where Tmodule : PartModule, IComparer<Tmodule>
    {
        private int vesselPartCount = -1;
        private Dictionary<KSPActionGroup, Tmodule> currentWinners = new Dictionary<KSPActionGroup, Tmodule>();

        /// <summary>
        /// Checks whether the specified module has a conflict for the specified action group.
        /// A conflict is defined as a situation where there's another module of the same
        /// type on the current vessel, and that module is assigned the same action group,
        /// and that module is a higher priority than the current one (according to the
        /// provided resolver).
        /// 
        /// Typically, such modules will implement their KSPAction in a way that checks
        /// HasConflict before doing anything. If HasConflict returns false, then they
        /// just do nothing and return.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="actionGroup"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public bool HasConflict(Tmodule module, KSPActionGroup actionGroup)
        {
            Vessel vessel = module.vessel;
            if (vessel == null)
            {
                currentWinners.Clear();
                return true;
            }
            if (vessel.parts.Count != vesselPartCount)
            {
                // Vessel structure has changed since we last checked (e.g.through staging or docking),
                // so we need to determine winners afresh.
                currentWinners.Clear();
                vesselPartCount = vessel.parts.Count;
            }
            Tmodule currentWinner = null;
            if (!currentWinners.TryGetValue(actionGroup, out currentWinner))
            {
                currentWinner = FindWinner(actionGroup, module);
                currentWinners.Add(actionGroup, currentWinner);
            }
            // It's a conflict if there exist any other modules of this time, *and* any of them
            // are higher-scoring than the current module.
            return (currentWinner != null) && !object.ReferenceEquals(module, currentWinner);
        }

        /// <summary>
        /// Iterates through the entire vessel, finding all Tmodules, and picks one
        /// as the winner. Returns null if no such modules can be found.
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        private Tmodule FindWinner(KSPActionGroup actionGroup, Tmodule comparer)
        {
            Vessel vessel = comparer.vessel;
            if (vessel == null) return null;
            Tmodule winner = null;
            for (int i = 0; i < vessel.parts.Count; ++i)
            {
                Part part = vessel.parts[i];
                for (int j = 0; j < part.Modules.Count; ++j)
                {
                    Tmodule module = part.Modules[j] as Tmodule;
                    if (module == null) continue;
                    if (!module.Actions.Contains(actionGroup)) continue;
                    if (winner == null)
                    {
                        winner = module;
                    }
                    else
                    {
                        winner = PickWinner(comparer, winner, module);
                    }
                }
            }
            return winner;
        }

        /// <summary>
        /// Given two modules that may conflict with each other, pick one as the winner.
        /// If the provided comparer declares a tie, then it'll use an arbitrary but
        /// deterministic way to guarantee that it'll be one or the other.
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="module1"></param>
        /// <param name="module2"></param>
        /// <returns></returns>
        private static Tmodule PickWinner(IComparer<Tmodule> comparer, Tmodule module1, Tmodule module2)
        {
            // Prefer the comparison supplied by the provided comparison.
            int comparison = comparer.Compare(module1, module2);
            if (comparison > 0) return module1;
            if (comparison < 0) return module2;

            // It's a tie, so we need a tiebreaker. If the two modules are on
            // different parts, prefer the one whose part has the lower persistent ID.
            int partIdComparison = module2.part.persistentId.CompareTo(module1.part.persistentId);
            if (comparison > 0) return module1;
            if (comparison < 0) return module2;

            // The modules are on the same part. Prefer whichever one comes earlier
            // in the modules list for the part.
            int indexComparison = module2.Index().CompareTo(module1.Index());
            return (indexComparison >= 0) ? module1 : module2;
        }
    }
}
