using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Last_Draw
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer gunSound;

        // Timer TO BE CHANGED    
        DispatcherTimer gameTimer;
        TimeSpan timeRemaining = TimeSpan.FromMinutes(3);


        private readonly string baseImagePath;
        private readonly string scenesPath;

        private string cassidyCharSelect;
        private string johnCharSelect;

        private string currentCharacter;
        private int currentScene = 1;

        private GameServer gameServer;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize base paths
            baseImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Images");
            scenesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Scenes");

            // Set character select images
            cassidyCharSelect = System.IO.Path.Combine(baseImagePath, "Cass", "Cassidy Knife.png");
            johnCharSelect = System.IO.Path.Combine(baseImagePath, "John", "John Knife.png");



            StartOpeningScreen();

            // Load the gunshot sound effect
            gunSound = new MediaPlayer();
            gunSound.Open(new Uri("Utilities/Sounds/Gun3.wav", UriKind.Relative)); // Pre-load the sound for faster playback
            this.PreviewMouseLeftButtonDown += Global_MouseClick;


            //Initialize timer
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromSeconds(1);
            gameTimer.Tick += UpdateTimer;

            // Start the game server in a separate thread
            InitializeServer();
        }

        // SERVER CONTROL!!!
        private void InitializeServer()
        {
            // Initialize the server
            gameServer = new GameServer();

            // Start the server in a new thread to avoid blocking the UI
            Thread serverThread = new Thread(() =>
            {
                try
                {
                    gameServer.StartServer();

                    // Update status to "Running" on success
                    Dispatcher.Invoke(() =>
                    {
                        ServerStatus.Text = "Server: Running";
                        ServerStatus.Foreground = Brushes.Green;
                    });
                }
                catch (Exception ex)
                {
                    // Update status to "Error" on failure
                    Dispatcher.Invoke(() =>
                    {
                        ServerStatus.Text = "Server: Error";
                        ServerStatus.Foreground = Brushes.Red;
                        MessageBox.Show($"Server Error: {ex.Message}", "Server Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });

            serverThread.IsBackground = true; // Mark thread as background to stop when the app closes
            serverThread.Start();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Stop the server if it's running
            gameServer?.StopServer();
        }

        // GAME CONTROL-------------------------------------------------------------------
        //-----------------------------------------------\
        private void StartOpeningScreen()
        {
            // Start Background Music
            BackgroundMusic.LoadedBehavior = MediaState.Manual;
            BackgroundMusic.Play();

            // Fade in Opening Image and Start Game Button
            FadeInElement(OpeningScreen, 5);
            FadeInElement(StartGameButton, 5);
        }
        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            // Fade out Opening Screen
            FadeOutElement(OpeningScreen, 1.5, () =>
            {
                OpeningScreen.Visibility = Visibility.Collapsed;
                PlayerSelectionScreen.Visibility = Visibility.Visible;

                // Optionally fade in Player Selection Screen
                FadeInElement(PlayerSelectionScreen, 1);
            });
        }

        private void CassidyButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame("Cassidy");
        }

        private void JohnButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame("John");
        }

        private void StartGame(string character)
        {

            currentCharacter = character;

            // Hide Player Selection Screen and Show Game Screen
            PlayerSelectionScreen.Visibility = Visibility.Collapsed;
            GameScreen.Visibility = Visibility.Visible;

            // Set the correct image based on the selected character
            string selectedCharacterImage = character == "Cassidy" ? cassidyCharSelect : johnCharSelect;

            // Update the character image in the HUD
            UpdateCharacterImage(selectedCharacterImage);

            // Fade out Player Selection Screen
            FadeOutElement(PlayerSelectionScreen, 1.5, () =>
            {
                PlayerSelectionScreen.Visibility = Visibility.Collapsed;
                GameScreen.Visibility = Visibility.Visible;

                //// Show the first scene for the selected character
                //GameScrenText.Text = $"Starting game as {character}";
            });

            // Start the global timer
            timeRemaining = TimeSpan.FromMinutes(3);
            gameTimer.Start();

            // Load the first scene
            LoadScene(1, character);


        }

        private void UpdateCharacterImage(string imagePath)
        {
            try
            {
                // Create BitmapImage with absolute path
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // Set the background
                CharSelectionRef.Background = new ImageBrush
                {
                    ImageSource = bitmap,
                    Stretch = Stretch.Uniform
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading character image: {ex.Message}\nPath: {imagePath}",
                              "Image Load Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // Helper Method: Fade in any UI element
        private void FadeInElement(UIElement element, double duration)
        {
            element.Visibility = Visibility.Visible;
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(duration)
            };
            element.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        }

        // Helper Method: Fade out any UI element with optional callback
        private void FadeOutElement(UIElement element, double duration, Action completedAction = null)
        {
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(duration)
            };

            fadeOutAnimation.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        }

        private void LoadScene(int sceneNumber, string character)
        {
            try
            {
                // Set current scene
                currentScene = sceneNumber;

                // Build the correct path using the Scenes folder
                string characterFolder = character == "Cassidy" ? "Cass" : "John";
                string backgroundPath = System.IO.Path.Combine(scenesPath, characterFolder, $"Scene{sceneNumber}.jpg");

                // Verify the file exists
                if (!File.Exists(backgroundPath))
                {
                    throw new FileNotFoundException($"Scene file not found: {backgroundPath}");
                }

                // Create and load the image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(backgroundPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                SceneFrame.Background = new ImageBrush
                {
                    ImageSource = bitmap,
                    Stretch = Stretch.UniformToFill
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading scene: {ex.Message}\nScene: {sceneNumber}\nCharacter: {character}",
                              "Scene Load Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }

            // Update HUD or attributes for the scene as needed
        }

        private void UpdateHud(int health, int audacity, int stealth, TimeSpan timeRemaining)
        {
            HealthText.Text = health.ToString();
            AudacityText.Text = audacity.ToString();
            StealthText.Text = stealth.ToString();
            TimerText.Text = $"Time: {timeRemaining:mm\\:ss}";
        }


        private void UpdateTimer(object sender, EventArgs e)
        {
            timeRemaining -= TimeSpan.FromSeconds(1);
            TimerText.Text = $"Time: {timeRemaining:mm\\:ss}";

            if (timeRemaining <= TimeSpan.Zero)
            {
                gameTimer.Stop();
                MessageBox.Show("Time's up! Game Over!");
            }
        }


        private void Global_MouseClick(object sender, MouseButtonEventArgs e)
        {
            // Play gun sound on any left-click
            gunSound.Stop();  // Reset sound position
            gunSound.Position = TimeSpan.Zero;
            gunSound.Play();
        }

    }
}
