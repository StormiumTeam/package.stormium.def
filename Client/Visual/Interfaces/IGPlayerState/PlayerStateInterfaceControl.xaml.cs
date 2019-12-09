using System.ComponentModel;
using System.Runtime.CompilerServices;
using Grid = Noesis.Grid;
#if !NOESIS
using System.Windows.Controls;
#else
using StormiumTeam.GameBase.Misc;
using Unity.Collections;
using Noesis;
using StormiumTeam.GameBase;
using UnityEngine;
using Unity.Entities;
using EventArgs = Noesis.EventArgs;
using GUI = Noesis.GUI;

#endif

namespace IGPlayerState_blend.Unity
{
    /// <summary>
    /// Logique d'interaction pour UserControl.xaml
    /// </summary>
    public partial class PlayerStateInterfaceControl : UserControl
    {
        private StaminaControl     m_StaminaControl;
        private DebugNetKeyControl m_DebugNetKeyControl;
        private Grid m_TestRectangle;

        public PlayerStateInterfaceControl()
        {
            Initialized += OnInitialized;
            InitializeComponent();
        }

#if NOESIS
        private void InitializeComponent()
        {
            World = World.Active;

            GUI.LoadComponent(this, @"Packages/package.stormium.default/Client/Visual/Interfaces/IGPlayerState/PlayerStateInterfaceControl.xaml");
        }
#endif

        private void OnInitialized(object sender, EventArgs args)
        {
            m_StaminaControl     = (StaminaControl) FindName("StaminaControl");
            m_DebugNetKeyControl = (DebugNetKeyControl) FindName("DebugNetKeyControl");
            m_TestRectangle = (Grid) FindName("FindThing");
        }

#if NOESIS
        public void OnUpdate(Entity localPlayer, CameraState cameraState, EntityQuery characterQuery)
        {
            m_StaminaControl.OnUpdate(World, cameraState.Target);
            m_DebugNetKeyControl.OnUpdate(World, cameraState.Target);

            using (var entities = characterQuery.ToEntityArray(Allocator.TempJob))
            {
                foreach (var ch in entities)
                {
                    if (cameraState.Target == ch)
                        continue;

                    var cam = World.GetExistingSystem<ClientCreateCameraSystem>().Camera;
                    
                    var viewport = cam.WorldToViewportPoint(new Vector3(2, 2, 2));
                    var margin = m_TestRectangle.Margin;
                    margin.Left = viewport.x * this.ActualWidth;
                    margin.Bottom = viewport.y * this.ActualHeight;
                    m_TestRectangle.Margin = margin;
                }
            }
        }
#endif
    }

    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}