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
using CharacterController;
using Noesis;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using EventArgs = Noesis.EventArgs;
using GUI = Noesis.GUI;
using Unity.Mathematics;
using Color = Noesis.Color;
#endif

namespace IGPlayerState_blend.Unity
{
    /// <summary>
    /// Logique d'interaction pour DebugNetKeyControl.xaml
    /// </summary>
    public partial class DebugNetKeyControl : UserControl
    {
        private TextBlock m_SpeedTextBlock;
        
        private Rectangle m_UpRectangle;
        private Rectangle m_DownRectangle;
        private Rectangle m_LeftRectangle;
        private Rectangle m_RightRectangle;

        private Rectangle m_DodgeRectangle;
        private Rectangle m_CrouchRectangle;
        private Rectangle m_JumpRectangle;

        private SolidColorBrush m_Active;
        private SolidColorBrush m_ClientActive;
        private SolidColorBrush m_NotActive;

        public DebugNetKeyControl()
        {
            InitializeComponent();
            Initialized += OnInitialized;
        }

        private void OnInitialized(object sender, EventArgs args)
        {
            m_SpeedTextBlock = (TextBlock) FindName("speed");
            
            m_UpRectangle    = (Rectangle) FindName("up");
            m_DownRectangle  = (Rectangle) FindName("down");
            m_LeftRectangle  = (Rectangle) FindName("left");
            m_RightRectangle = (Rectangle) FindName("right");

            m_DodgeRectangle  = (Rectangle) FindName("dodge");
            m_CrouchRectangle = (Rectangle) FindName("crouch");
            m_JumpRectangle   = (Rectangle) FindName("jump");

            m_Active       = new SolidColorBrush(Color.FromRgb(249, 249, 249));
            m_ClientActive = new SolidColorBrush(Color.FromRgb(125, 100, 255));
            m_NotActive    = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        }

#if NOESIS
        private void InitializeComponent()
        {
            GUI.LoadComponent(this, @"Packages/package.stormium.default/Client/Visual/Interfaces/IGPlayerState/DebugNetKeyControl.xaml");
        }

        private float m_PreviousSpeed;

        public void OnUpdate(World world, Entity spectated)
        {
            if (world == null || spectated == default)
                return;

            if (!world.EntityManager.HasComponent<CharacterInput>(spectated))
                return;

            var                   input       = world.EntityManager.GetComponentData<CharacterInput>(spectated);
            var                   hasPlayer   = false;
            GamePlayerUserCommand userCommand = default;
            if (world.EntityManager.HasComponent<Relative<PlayerDescription>>(spectated))
            {
                var player = world.EntityManager.GetComponentData<Relative<PlayerDescription>>(spectated).Target;
                userCommand = world.EntityManager.GetComponentData<GamePlayerUserCommand>(player);

                hasPlayer = true;
            }

            if (world.EntityManager.HasComponent<Velocity>(spectated))
            {
                var velocity  = world.EntityManager.GetComponentData<Velocity>(spectated);
                var currSpeed = math.length(velocity.xfz);
                if (math.abs(m_PreviousSpeed - currSpeed) > 0.1f)
                {
                    m_PreviousSpeed = math.length(velocity.xfz);
                    m_SpeedTextBlock.Text = ((int)(currSpeed * 10) * 0.1f).ToString("F1");
                }
            }
            else
            {
                m_SpeedTextBlock.Text = string.Empty;
            }

            m_UpRectangle.Fill = input.Move.y >= 0.5f     ? m_Active
                : hasPlayer && userCommand.Move.y >= 0.5f ? m_ClientActive : m_NotActive;

            m_DownRectangle.Fill = input.Move.y <= -0.5f   ? m_Active
                : hasPlayer && userCommand.Move.y <= -0.5f ? m_ClientActive : m_NotActive;

            m_RightRectangle.Fill = input.Move.x >= 0.5f  ? m_Active
                : hasPlayer && userCommand.Move.x >= 0.5f ? m_ClientActive : m_NotActive;

            m_LeftRectangle.Fill = input.Move.x <= -0.5f   ? m_Active
                : hasPlayer && userCommand.Move.x <= -0.5f ? m_ClientActive : m_NotActive;


            m_DodgeRectangle.Fill = input.Dodge      ? m_Active
                : hasPlayer && userCommand.IsDodging ? m_ClientActive : m_NotActive;

            m_CrouchRectangle.Fill = input.Crouch      ? m_Active
                : hasPlayer && userCommand.IsCrouching ? m_ClientActive : m_NotActive;

            m_JumpRectangle.Fill = input.Jump        ? m_Active
                : hasPlayer && userCommand.IsJumping ? m_ClientActive : m_NotActive;
        }
#endif
    }
}