using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

//using UnityEngine.Rendering.PostProcessing;

namespace package.stormiumteam.shared.settings
{
    [CreateAssetMenu(fileName = "GfxProfile", menuName = "Stormium/Gfx Profile", order = 0)]
    public class StDefGfxProfile : ScriptableObject
    {
        public string Name;
        public RenderPipelineAsset SrpAsset;
        //public PostProcessProfile GlobalFxProfile;
    }
}