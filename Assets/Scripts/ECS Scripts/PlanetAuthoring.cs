using Unity.Entities;
using UnityEngine;

namespace ECS_Scripts
{
    public class PlanetAuthoring : MonoBehaviour
    {
        public Vector3d StartPosition;
        public Vector3d StartVelocity;
        public double ConstantRotationSpeed;
        public double TotalMass;

        class Baker : Baker<PlanetAuthoring>
        {
            public override void Bake(PlanetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlanetData()
                {
                    Position = authoring.StartPosition,
                    Velocity = authoring.StartVelocity,
                    RotationSpeed = authoring.ConstantRotationSpeed,
                    Mass = authoring.TotalMass,
                    Acceleration = new Vector3d()
                });
            } 
        }
    }

    public struct PlanetData : IComponentData
    {
        public Vector3d Position;
        public Vector3d Velocity;
        public double RotationSpeed;
        public double Mass;
        public Vector3d Acceleration;
    }
}