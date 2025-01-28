using System.Configuration;
using System.Data;
using System.IO;
using System.Media;
using System.Windows;

namespace noughts_and_crosses
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static App MainApp => ((App)Current);
        public bool soundsOn;
        public Sound moveSound;
        public Sound winSound;

        public List<Theme> themes = new List<Theme>();
        private Theme _currentTheme;
        public Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                ResourceDictionary styles = new ResourceDictionary() { Source = new Uri("Styles.xaml", UriKind.Relative) };
                ResourceDictionary theme = new ResourceDictionary() { Source = new Uri(_currentTheme.Path, UriKind.Relative) };
                theme.MergedDictionaries.Add(styles);
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(theme);
            }
        }

        public App()
        {
            InitializeComponent();

            themes.Add(new Theme() { Name = "Light", Path = "Themes/Light.xaml" });
            themes.Add(new Theme() { Name = "Dark", Path = "Themes/Dark.xaml" });
            themes.Add(new Theme() { Name = "High Contrast Light", Path = "Themes/HighContrastLight.xaml" });
            themes.Add(new Theme() { Name = "High Contrast Dark", Path = "Themes/HighContrastDark.xaml" });
            themes.Add(new Theme() { Name = "Colourful", Path = "Themes/Colourful.xaml" });
            themes.Add(new Theme() { Name = "Industrial", Path = "Themes/Factorio.xaml" });
            themes.Add(new Theme() { Name = "Space Age", Path = "Themes/Kerbal.xaml" });
            themes.Add(new Theme() { Name = "Programmer", Path = "Themes/Perry.xaml" });
            CurrentTheme = themes[0];

            soundsOn = true;
            moveSound = new Sound("pack://application:,,,/Themes/Sounds/move.wav");
            winSound = new Sound("pack://application:,,,/Themes/Sounds/win.wav");
        }
    }


    public class Theme
    {
        public string Name;
        public string Path;
    }

    public class Sound
    {
        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                _path = value;
                Uri uri = new Uri(_path, UriKind.Absolute);
                Stream resourceStream = Application.GetResourceStream(uri).Stream;
                player = new SoundPlayer(resourceStream);
            }
        }
        private SoundPlayer player;

        public Sound(string newPath)
        {
            Path = newPath;
        }

        public void Play()
        {
            if (App.MainApp.soundsOn)
            {
                player.Play();
            }
        }

        public void Stop()
        {
            player.Stop();
        }
    }
}
