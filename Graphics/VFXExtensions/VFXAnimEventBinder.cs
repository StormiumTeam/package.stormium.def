namespace UnityEngine.VFX.Utility
{
	public class VFXAnimEventBinder : VFXEventBinderBase
	{
		public void SendEvent()
		{
			SendEventToVisualEffect();
		}

		protected override void SetEventAttribute(object[] parameters = null)
		{
			
		}
	}
}