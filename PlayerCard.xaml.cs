using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace noughts_and_crosses
{
    /// <summary>
    /// Interaction logic for PlayerCard.xaml
    /// </summary>
    public partial class PlayerCard : UserControl
    {
        public event EventHandler DeletePlayer = delegate { };
        public event EventHandler UpdatePlayer = delegate { };

        private string _playerName;
        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                UsernameTextbox.Text = value;
            }
        }

        public PlayerCard()
        {
            InitializeComponent();
            _playerName = UsernameTextbox.Text;
        }

        private void DeleteButtonClicked(object sender, RoutedEventArgs e)
        {
            if (DeletePlayer != null)
            {
                DeletePlayer(this, e);
            }
        }

        private void PlayerNameChanged(object sender, EventArgs e)
        {
            _playerName = UsernameTextbox.Text.Trim();
            if (UpdatePlayer != null)
            {
                UpdatePlayer(this, e);
            }
        }
    }
}
