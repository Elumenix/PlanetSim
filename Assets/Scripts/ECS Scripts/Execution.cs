using Unity.Entities;
using UnityEngine;

namespace ECS_Scripts
{
    public class Execution : MonoBehaviour
    {
        public bool OrbitPlanets;

        class Baker : Baker<Execution>
        {
            // What data the given object should subscribe too
            public override void Bake(Execution execution)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (execution.OrbitPlanets) AddComponent<ExecuteOrbitPlanets>(entity);
            }
        }
    }

    public struct ExecuteOrbitPlanets : IComponentData {}
}
