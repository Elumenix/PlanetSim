using Unity.Burst;
using Unity.Entities;

namespace ECS_Scripts
{
    // Force update to used fixed delta time
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PlanetHandler : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteOrbitPlanets>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            int timeDir = 1; // PlaceHolder while gravitational body isn't implemented

            EntityQuery planetQuery = state.GetEntityQuery(ComponentType.ReadWrite<PlanetData>());
            
            // First perform GravityManager Calculations (Runge Kutta)
            
            
            // Do the simple GravitationalBody Update 
            
            
            //foreach (var (transform) in SystemAPI.Query<)
        }

        /*[BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }*/
    }
}
