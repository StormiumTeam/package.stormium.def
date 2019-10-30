using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows;
#else
using Noesis;
using Revolution;
using Revolution.NetCode;
using RPCs;
using StormiumTeam.GameBase;
using UnityEngine;
using Unity.Entities;
using EventArgs = Noesis.EventArgs;
using GUI = Noesis.GUI;
using Path = Noesis.Path;
using Unity.Mathematics;
#endif

namespace IGDeathMatch_blend.Unity
{
    /// <summary>
    /// Logique d'interaction pour UserControl.xaml
    /// </summary>
    public partial class DeathMatchInterfaceControl : UserControl
    {
        private TextBlock m_ChronoLabel;
        private ViewModel m_ViewModel;

        public ViewModel ViewModel => m_ViewModel;

        public DeathMatchInterfaceControl()
        {
            Initialized += OnInitialized;
            InitializeComponent();
        }

#if NOESIS
        private void InitializeComponent()
        {
            World = World.Active;

            GUI.LoadComponent(this, @"Packages\package.stormium.def\Client\Visual\Interfaces\IGDeathMatch\DeathMatchInterfaceControl.xaml");
        }
#endif

        private void OnInitialized(object sender, EventArgs args)
        {
            DataContext = m_ViewModel = new ViewModel();

            m_ViewModel.Countdown = "00:00";
            
            m_ViewModel.PlayerSpectators = new ObservableCollection<PlayerSpectator>();

#if !NOESIS
            m_ViewModel.PlayerSpectators = new ObservableCollection<PlayerSpectator>();
            m_ViewModel.PlayerSpectators.Add(new PlayerSpectator { Content = "Player#1" });
            m_ViewModel.PlayerSpectators.Add(new PlayerSpectator { Content = "Player#2" });
            m_ViewModel.PlayerSpectators.Add(new PlayerSpectator { Content = "Player#3" });
#endif
        }

#if NOESIS
        protected override bool ConnectEvent(object source, string eventName, string handlerName)
        {
            if (eventName == "Click" && handlerName == "SpectatorChoosePlayer")
            {
                ((Button) source).Click += SpectatorChoosePlayer;
                return true;
            }

            return false;
        }
        private TimeSpan m_PreviousTimeSpan;

        public void OnUpdate(int time, int startTime, int endTime)
        {
            var span = new TimeSpan((int) math.select(endTime - time, startTime + time, endTime < time) * TimeSpan.TicksPerMillisecond);
            if (span.Seconds != m_PreviousTimeSpan.Seconds)
            {
                m_PreviousTimeSpan = span;

                m_ViewModel.Countdown = span.ToString("mm\\:ss");
            }
        }
#endif

        private void SpectatorChoosePlayer(object sender, RoutedEventArgs args)
        {
#if NOESIS
            var button = (Button)sender;
            if (button.Content is PlayerSpectator playerSpectator)
            {
                
                var ghostTargetId = World.EntityManager.GetComponentData<ReplicatedEntity>(playerSpectator.Entity).GhostId;
                var reqEnt        = World.EntityManager.CreateEntity(typeof(RequestToSpectateEntityRpc), typeof(SendRpcCommandRequestComponent));
                World.EntityManager.SetComponentData(reqEnt, new RequestToSpectateEntityRpc {GhostTarget = ghostTargetId});
            }
#endif
        }
    }

    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ViewModel : NotifyPropertyChangedBase
    {
        private string m_Countdown;

        public string Countdown
        {
            get => m_Countdown;
            set
            {
                m_Countdown = value;
                OnPropertyChanged(nameof(Countdown));
            }
        }

        private ObservableCollection<PlayerSpectator> m_PlayerSpectators;

        public ObservableCollection<PlayerSpectator> PlayerSpectators
        {
            get => m_PlayerSpectators;
            set
            {
                if (m_PlayerSpectators != value)
                {
                    m_PlayerSpectators = value;
                    OnPropertyChanged(nameof(PlayerSpectator));
                }
            }
        }
    }

    public class PlayerSpectator
    {
#if NOESIS
        public Entity Entity { get; set; }
#endif
        public string Content { get; set; }

        public override string ToString()
        {
            return Content;
        }
    }
}