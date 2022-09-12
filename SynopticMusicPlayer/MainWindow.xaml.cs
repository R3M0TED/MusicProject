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
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Media;

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
        public MainWindow()
        {
            InitializeComponent();
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

                var query = "SELECT Directory FROM Song WHERE Id = " + index;

                using (connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(query, connection);
                    string dir = (string)command.ExecuteScalar();
                    connection.Close();
                    MessageBox.Show(dir + " "+ index);
                    playSelectedSong(dir);

                }

            }
        }
        private void playSelectedSong(string dir)
        {
            player.Open(new Uri(dir));
            player.Play();
            Console.WriteLine("test");
        }


}
}
