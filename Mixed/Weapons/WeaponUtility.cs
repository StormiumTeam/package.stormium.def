using Stormium.Core;
using StormiumTeam.GameBase;

namespace Stormium.Default.Mixed
{
	public static class WeaponUtility
	{
		public enum ReloadUpdateState
		{
			None,
			Updating,
			Finished,
			Canceled
		}

		public static bool TriggerReload(this ref ReloadingState state, bool isReloadingRequested, int ammo)
		{
			if ((isReloadingRequested || ammo <= 0) && !state.Active)
			{
				state.Active = true;
				state.Progress.Reset();
				return true;
			}

			return false;
		}

		public static ReloadUpdateState UpdateReloadAndGetState(this ref ReloadingState state, int ammo, bool cancel, in UTick tick)
		{
			if (state.Active && (!cancel || ammo == 0))
			{
				state.Progress += tick;
				if (state.Progress.Value >= state.TimeToReload)
				{
					state.Progress.Reset();
					state.Active = false;
					return ReloadUpdateState.Finished;
				}

				return ReloadUpdateState.Updating;
			}

			if (ammo > 0)
			{
				state.Progress.Reset();
				state.Active = false;
				return ReloadUpdateState.Canceled;
			}

			return ReloadUpdateState.None;
		}
	}
}