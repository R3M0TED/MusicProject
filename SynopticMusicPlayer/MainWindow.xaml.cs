using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Media.Imaging;

namespace SynopticMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection connection;
        string connectionString;
        MediaPlayer player;
        bool musicPlaying;
        bool songSelected;

        public MainWindow()
        {
            InitializeComponent();
            setUp();
            connectionString = ConfigurationManager.ConnectionStrings["SynopticMusicPlayer.Properties.Settings.SongsConnectionString"].ConnectionString;
            PopulateSongs();
        }

        private void PopulateSongs()
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Song", connection))
            {
                DataTable songTable = new DataTable();
                adapter.Fill(songTable);
                songsDataGrid.ItemsSource = songTable.DefaultView;
            }
        }

        private void songsDataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var index = dataGrid.SelectedIndex + 2;
                string dir = directoryReturner(index);
                player.Open(new Uri(dir));
                musicPlaying = true;
                songSelected = true;
                helpIndicator.Visibility = Visibility.Hidden;
                playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                player.Play();                                           
            }
        }

        private string directoryReturner(int index)
        {
            var query = "SELECT Directory FROM Song WHERE Id = " + index;

            using (connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                string dir = (string)command.ExecuteScalar();
                connection.Close();
                MessageBox.Show(dir);
                return dir;
            }

        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = volumeSlider.Value / 100;
        }

        private void setUp()
        {
            player = new MediaPlayer();
            player.Volume = 0;
            volumeSlider.Value = 25;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(musicPlaying == true)//user pauses
            {
                playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                musicPlaying = false;
                player.Pause();
            }
            else if(musicPlaying == false && songSelected == true)//user resumes
            {
                playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));

                musicPlaying = true;
                player.Play();
            }
            else
            {
                helpIndicator.Visibility = Visibility.Visible;
            }
        }
    }
}
