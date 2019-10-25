using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NOESIS
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
#else
using Noesis;
using Stormium.Default;
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
    /// Logique d'interaction pour StaminaControl.xaml
    /// </summary>
    public partial class StaminaControl : UserControl
    {
        private ViewModel m_ViewModel;
        private Border m_BorderQuad;

        public StaminaControl()
        {
            InitializeComponent();
        }

#if NOESIS
        private void InitializeComponent()
        {
            GUI.LoadComponent(this, @"Packages/package.stormium.default/Client/Visual/Interfaces/IGPlayerState/StaminaControl.xaml");

            DataContext = m_ViewModel = new ViewModel();
            m_HideAt = 1f;
            m_BorderQuad = (Border) FindName("BorderQuad");
        }

        private float m_HideProgression;
        private float m_HideAt;

        public void OnUpdate(World world, Entity spectated)
        {
            if (world == null || spectated == default)
                return;

            if (!world.EntityManager.HasComponent<Stamina>(spectated))
                return;

            var stamina           = world.EntityManager.GetComponentData<Stamina>(spectated);
            var staminaPercentage = stamina.Value == default || stamina.Max == default ? 0 : (int) (stamina.Value / stamina.Max * 100);
            var staminaPercentageStrict = stamina.Value == default || stamina.Max == default ? 0 : stamina.Value / stamina.Max;
            if (staminaPercentage != m_ViewModel.StaminaValue)
            {
                m_ViewModel.StaminaValue = staminaPercentage;
            }

            var parentWidth = m_BorderQuad.Parent.ActualWidth;
            var wantedWidth = staminaPercentageStrict * parentWidth;
            var target = Mathf.MoveTowards(math.lerp(m_BorderQuad.Width,wantedWidth, Time.deltaTime * 12.5f), wantedWidth, Time.deltaTime * 7.5f);
            {
                target = math.clamp(target, 0, parentWidth);
            }
            m_BorderQuad.Width = target;

            var targetOpacity = staminaPercentage == 100 ? 0 : 1;
            if (targetOpacity == 0)
                m_HideProgression += Time.deltaTime;
            else
                m_HideProgression = 0;

            if (targetOpacity == 0 && m_HideAt > m_HideProgression)
                targetOpacity = 1;
            
            Opacity = Mathf.MoveTowards(Opacity, targetOpacity, Time.deltaTime * 5f);
        }
#endif

        public class ViewModel : NotifyPropertyChangedBase
        {
            private int m_StaminaValue;

            public int StaminaValue
            {
                get => m_StaminaValue;
                set
                {
                    m_StaminaValue = value;
                    OnPropertyChanged(nameof(StaminaValue));
                }
            }
        }
    }
}