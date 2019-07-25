using Unity.Entities;
using UnityEngine;

namespace GUIScripts
{
	public class UIWorldHealthEntityAuthoring : MonoBehaviour, IConvertGameObjectToEntity
	{
		public UIWorldHealthEntity UIComponent;
		
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			UIComponent.Target = entity;
		}
	}
}