using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ThirdParty.UnityHDRP_Extensions
{
    [ExecuteAlways]
    public class DecalGetWorldPosition : MonoBehaviour
    {
        private Material m_Instance;
        private int m_PositionNameId;
        
        public DecalProjector DecalProjector;
        public Material Material;

        void OnEnable()
        {
            if (!RegenInstance())
                return;
            
            DecalProjector.material = m_Instance;
        }

        void OnDisable()
        {
            Destroy(m_Instance);
        }

        void Update()
        {
            m_Instance.SetVector(m_PositionNameId, transform.position);
        }

        void OnValidate()
        {
            if (!RegenInstance())
                return;

            DecalProjector.material = m_Instance;
        }

        bool RegenInstance()
        {
            if (m_Instance != null)
                Destroy(m_Instance);

            m_Instance = Instantiate(Material);
            m_PositionNameId = Shader.PropertyToID("_Position");
            
            return m_Instance != null;
        }
    }
}