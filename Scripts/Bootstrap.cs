using package.stormiumteam.shared.modding;

namespace package.stormium.def
{
    public class Bootstrap : CModBootstrap
    {
        protected override void OnRegister()
        {
        }

        protected override void OnUnregister()
        {

        }

        public static void register()
        {
            new Bootstrap().OnRegister();
        }
    }
}