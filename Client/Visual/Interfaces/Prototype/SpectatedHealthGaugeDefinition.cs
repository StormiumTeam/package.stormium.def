using StormiumTeam.GameBase.Components;

namespace Stormium.Default.Client.Visual.Interfaces.Prototype
{
	public class SpectatedHealthGaugeDefinition : GaugeDefinition
	{

	}

	public class SpectatedHealthGaugeRender : RenderSystem<SpectatedHealthGaugeDefinition>
	{
		public int Value, Max;

		public override void PrepareValues()
		{
			var gamePlayer = GetFirstSelfGamePlayer();
			if (gamePlayer == default)
				return;

			if (!TryGetCurrentCameraState(gamePlayer, out var camState))
				return;

			if (!EntityManager.HasComponent<LivableHealth>(camState.Target))
				return;

			var livableHealth = EntityManager.GetComponentData<LivableHealth>(camState.Target);
			Value = livableHealth.Value;
			Max   = livableHealth.Max;
		}

		public override void Render(SpectatedHealthGaugeDefinition definition)
		{
			definition.Set(Value, Max);
		}

		public override void ClearValues()
		{
			Value = default;
			Max   = default;
		}
	}
}