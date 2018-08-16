using System.Collections.Generic;
using System.Collections.ObjectModel;
using package.stormiumteam.shared.settings;
using Unity.Entities;
using UnityEngine.Experimental.Rendering;

namespace package.stormium.def.settings
{
    public class StDefGfxProfileManager : ComponentSystem
    {
        private string m_ActiveSettings;
        private Dictionary<string, StDefGfxProfile> m_AllProfiles;

        public StDefGfxProfile ActiveSettings => GetProfile(m_ActiveSettings, out var hadProfile);
        public ReadOnlyDictionary<string, StDefGfxProfile> Profiles 
            => new ReadOnlyDictionary<string, StDefGfxProfile>(m_AllProfiles);

        public StDefGfxProfile GetProfile(string name, out bool hadProfile)
        {
            if (m_AllProfiles.ContainsKey(name))
            {
                hadProfile = true;
                return m_AllProfiles[name];
            }
            
            hadProfile = false;
            return default(StDefGfxProfile);
        }
        
        public void SetSettings(StDefGfxProfile gfxSettings)
        {
            
        }

        public void SaveSettings(StDefGfxProfile gfxSettings)
        {
            
        }

        protected override void OnCreateManager(int capacity)
        {
            m_AllProfiles = new Dictionary<string, StDefGfxProfile>()
            {
                {
                    "default_low",
                    new StDefGfxProfile()
                    {
                        Name = "default_low",
                    }
                },
                {
                    "default_medium",
                    new StDefGfxProfile()
                    {
                        Name = "default_medium",
                    }
                },
                {
                    "default_high",
                    new StDefGfxProfile()
                    {
                        Name = "default_high",
                    }
                },
            };
        }

        protected override void OnDestroyManager()
        {
            m_AllProfiles.Clear();
            m_AllProfiles = null;
        }

        protected override void OnUpdate()
        {
            
        }
    }
}