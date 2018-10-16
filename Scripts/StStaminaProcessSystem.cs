using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace package.stormium.def
{
    using static Unity.Mathematics.math;
    
    public class StStaminaProcessSystem : GameJobComponentSystem
    {
        private struct JobUpdateStamina : IJobProcessComponentData<StStamina>
        {
            [ReadOnly] public float DeltaTime;
            
            public void Execute(ref StStamina data)
            {
                data.Value = clamp(data.Value + (data.Gain * DeltaTime), 0, data.Max);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var dt = Time.deltaTime;
            
            // TODO: Should we use a fixed time or the frame dependant time?
            return new JobUpdateStamina()
            {
                DeltaTime = dt
            }.Schedule(this, inputDeps);
        }
    }
}