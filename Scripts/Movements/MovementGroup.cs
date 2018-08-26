using Unity.Entities;

namespace package.stormium.def.Movements
{
    public class MovementGroup : BarrierSystem
    {
        [UpdateAfter(typeof(MovementGroup))]
        public class ManageInput : BarrierSystem
        {
            
        }
        
        [UpdateAfter(typeof(MovementGroup))]
        public class ManageCooldown : BarrierSystem
        {
            
        }

        [UpdateAfter(typeof(ManageCooldown))]
        public class ManageMovement : BarrierSystem
        {
            
        }
    }
}