using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
#if !NOESIS
using System.Windows.Controls;
#else
using Noesis;
using StormiumTeam.GameBase;
using UnityEngine;
using Unity.Entities;
using EventArgs = Noesis.EventArgs;
using GUI = Noesis.GUI;
using Path = Noesis.Path;
using Unity.Mathematics;
#endif

namespace IGPlayerState_blend.Unity
{
    /// <summary>
    /// Logique d'interaction pour UserControl.xaml
    /// </summary>
    public partial class PlayerStateInterfaceControl : UserControl
    {
        private StaminaControl m_StaminaControl;
        
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
            m_StaminaControl = (StaminaControl) FindName("StaminaControl");
        }

#if NOESIS
        private TimeSpan m_PreviousTimeSpan;

        public void OnUpdate(Entity localPlayer, CameraState cameraState)
        {
            m_StaminaControl.OnUpdate(World, cameraState.Target);
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