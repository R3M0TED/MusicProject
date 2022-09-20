using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Controls.Primitives;

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
        DataTable playlistViewer;
        bool musicPlaying;
        bool songSelected;
        bool isCreatingPlaylist;
        int currentSongID;
        int rightClickedSongID;
        int tableIndex;
        string playlistName;
        string playlistIDs = "";
        string folderLocation = @"C:\Users\user\source\repos\SynopticMusicPlayer\MusicProject\SynopticMusicPlayer\Music\";


        public MainWindow()
        {
            connectionString = ConfigurationManager.ConnectionStrings["SynopticMusicPlayer.Properties.Settings.SongsConnectionString"].ConnectionString;

            InitializeComponent();
            setUp();
            PopulateSongs();
        }


        private void PopulateSongs() //Populate DataGrid with ALL SONGS
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs", connection))
            {
                DataTable songTable = new DataTable();
                adapter.Fill(songTable);
                songsDataGrid.ItemsSource = songTable.DefaultView;
            }
        }


        private string directoryReturner(int index)
        {
            var query = "SELECT Directory FROM Songs WHERE Id = " + index;

            using (connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                string dir = (string)command.ExecuteScalar();
                connection.Close();
                dir = folderLocation + dir;
                //MessageBox.Show(dir);
                return dir;
            }
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = volumeSlider.Value / 100;
        }

        private void setUp()
        {
            checkForUpdates();
            playlistPickerComboBox.Items.Add("All Songs");
            loadPlaylistNames(playlistPickerComboBox);

            player = new MediaPlayer();
            player.Volume = 0;
            volumeSlider.Value = 25;

        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (isCreatingPlaylist == false)
            {
                if (musicPlaying == true)//user pauses
                {
                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                    musicPlaying = false;
                    player.Pause();
                }
                else if (musicPlaying == false && songSelected == true)//user resumes
                {
                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));

                    musicPlaying = true;
                    player.Play();
                }
                else
                {
                    MessageBox.Show("Please select a song");
                }
            }
            else
            {
                MessageBox.Show("You are currently creating a playlist");
            }
        }



        private void nextBtnImage_MouseDown(object sender, MouseButtonEventArgs e)
        {

            int index = currentSongID;

            if ((tableIndex + 1) < songsDataGrid.Items.Count && songSelected == true)
            {
                try
                {
                    tableIndex = tableIndex + 1;

                    TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                    currentSongID = Int32.Parse(id.Text);
                    string dir = directoryReturner(Int32.Parse(id.Text));
                    //MessageBox.Show(id.Text); //outputs 2

                    if (dir != null && isCreatingPlaylist == false)
                    {

                        player.Open(new Uri(dir));
                        musicPlaying = true;
                        songSelected = true;
                        playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                        songsDataGrid.CurrentItem = index;
                        updateCurrentlyPlayingLabel(tableIndex);
                        playbackSlider.Value = 0;
                        player.Play();
                    }
                    else
                    {
                        if (dir == null)
                        {

                        }
                        else if (isCreatingPlaylist == true)
                        {
                            MessageBox.Show("You are currently creating a playlist");
                        }

                    }
                }
                catch
                {
                    tableIndex = 0;
                }
            }
        }

        private void prevBtnImg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int index = currentSongID;

            if ((tableIndex - 1) >= 0 && songSelected == true)
            {
                try
                {
                    tableIndex = tableIndex - 1;
                    TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                    currentSongID = Int32.Parse(id.Text);
                    string dir = directoryReturner(Int32.Parse(id.Text));
                    //MessageBox.Show(id.Text); //outputs 2


                    if (dir != null && isCreatingPlaylist == false)
                    {
                        player.Open(new Uri(dir));
                        musicPlaying = true;
                        songSelected = true;
                        playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                        songsDataGrid.CurrentItem = index;
                        updateCurrentlyPlayingLabel(tableIndex);
                        playbackSlider.Value = 0;

                        player.Play();
                    }
                    else
                    {
                        if (dir == null)
                        {

                        }
                        else if (isCreatingPlaylist == true)
                        {
                            MessageBox.Show("You are currently creating a playlist");
                        }
                    }
                }
                catch
                {
                    tableIndex = 0;
                }
        } }




        private void savePlaylistNameBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistName = playlistNameTxtBox.Text;
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand nameCheck = connection.CreateCommand();
                    nameCheck.CommandText = "SELECT* FROM Playlist WHERE PlaylistName = @playlistName";
                    nameCheck.Parameters.AddWithValue("@playlistName", playlistName);
                    if (nameCheck.ExecuteScalar() == null)
                    {
                        playlistNameWindow.IsOpen = false;
                        finishNewPlaylistBtn.Visibility = Visibility.Visible;
                        viewCurrentPlaylistBtn.Visibility = Visibility.Visible;
                        cancelNewPlaylistBtn.Visibility = Visibility.Visible;
                        playlistPickerComboBox.IsEnabled = false;
                        isCreatingPlaylist = true;
                        playlistViewer = new DataTable();
                        playlistViewer.Columns.Add("Name");
                        playlistViewer.Columns.Add("Artist");
                        playlistViewer.Columns.Add("ID");
                        playlistSongPreviewerDataGrid.ItemsSource = playlistViewer.DefaultView;
                    }
                    else
                    {
                        playlistNameTxtBox.Text = "Duplicate Name";
                    }
                }
            }
            else
            {
                playlistName = null;
                playlistNameTxtBox.Text = "Invalid Name";

            }

        }






        private void playlistPickerComboBox_DropDownClosed(object sender, EventArgs e)
        {
            updateDataGrid();

        }

        private void updateDataGrid()
        {
            var playlistName = playlistPickerComboBox.SelectedValue;

            if (playlistName.ToString() != "All Songs")
            {
                var query = "SELECT [Song ID] FROM Playlist WHERE PlaylistName='" + playlistName + "'";

                //MessageBox.Show(selection.ToString());
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            reader.Read();
                            var str = reader.GetString(0);
                            if (str != null)
                            {
                                //var idData = str.Split(',');
                                string[] newData = str.Split(',');
                                //MessageBox.Show(newData);
                                DataTable songTable = new DataTable();

                                for (int i = 0; i < newData.Length; i++)
                                {
                                    if (!String.IsNullOrWhiteSpace(newData[i]))
                                    {
                                        reader.Close();
                                        using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs WHERE Id=" + newData[i], connection))
                                        {
                                            adapter.Fill(songTable);
                                            songsDataGrid.ItemsSource = songTable.DefaultView;
                                            musicPlaying = false;
                                            updateCurrentlyPlayingLabel(0);
                                            songSelected = false;
                                            playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                                            playbackSlider.Value = 0;
                                            player.Close();
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Null exception");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                PopulateSongs();
            }
        }

        private void checkForUpdates()
        {
            string path = @"C:\Users\user\source\repos\SynopticMusicPlayer\MusicProject\SynopticMusicPlayer\Music";
            int fileCount = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Length;
            int rowCount = 0;
            string rowCountQuery = "SELECT COUNT(*) FROM Songs";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand count = new SqlCommand(rowCountQuery, connection);
                rowCount = (int)count.ExecuteScalar();
                connection.Close();
            }




            if (fileCount != rowCount)
            {
                string[] fileEntries = Directory.GetFiles(path);

                for (int i = 0; i < fileEntries.Length; i++)
                {
                    string filename = Path.GetFileName(fileEntries[i]);

                    string returnFileNameQuery = "SELECT * FROM Songs WHERE Directory = '" + filename + "'";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlCommand search = new SqlCommand(returnFileNameQuery, connection);


                        if (search.ExecuteScalar() == null)
                        {
                            var file = TagLib.File.Create(fileEntries[i]);
                            var songTitle = file.Tag.Title;
                            var songArtists = "";
                            if (songTitle == null)//blank title use file name
                            {
                                songTitle = filename;
                            }

                            string[] songArtistArray = file.Tag.AlbumArtists;

                            if (songArtistArray.Length == 0)
                            {
                                songArtists = "Blank";
                            }
                            else
                            {
                                songArtists = songArtistArray[0];
                            }



                            SqlCommand addSong = connection.CreateCommand();
                            addSong.Parameters.AddWithValue("@name", songTitle);
                            addSong.Parameters.AddWithValue("@artist", songArtists);
                            addSong.Parameters.AddWithValue("@dir", filename);

                            //SqlTransaction transaction;
                            //transaction = connection.BeginTransaction();
                            //addSong.Connection = connection;
                            //addSong.Transaction = transaction;
                            try
                            {
                                addSong.CommandText = "INSERT INTO Songs(Name, Artist, Directory) VALUES(@name, @artist, @dir)";
                                addSong.ExecuteNonQuery();
                                //transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error");

                                Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                                Console.WriteLine("  Message: {0}", ex.Message);
                                try //Attempt to roll back transaction
                                {
                                    //transaction.Rollback();
                                }
                                catch (Exception ex2)
                                {
                                    Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                                    Console.WriteLine("  Message: {0}", ex2.Message);
                                }
                            }
                        }

                    }


                }
            }
        }
        private void updateCurrentlyPlayingLabel(int tableIndex)
        {

            if (musicPlaying == true)
            {
                TextBlock name = songsDataGrid.Columns[0].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                TextBlock artist = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                currentlyPlayingLabel.Content = name.Text + " - " + artist.Text;

            }
            else
            {
                currentlyPlayingLabel.Content = "Nothing";

            }
        }



        private void shuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            musicPlaying = true;
            songSelected = true;
            int num = rnd.Next(0, songsDataGrid.Items.Count);
            TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[num]) as TextBlock;//Save Song ID from selected row index
            tableIndex = num;
            var dir = directoryReturner(Int32.Parse(id.Text));
            currentSongID = Int32.Parse(id.Text);
            playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
            player.Open(new Uri(dir));
            updateCurrentlyPlayingLabel(num);
            player.Play();
        }



        //private void testBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    double pos = player.Position.TotalSeconds;
        //    double length = player.NaturalDuration.TimeSpan.TotalSeconds;
        //    MessageBox.Show(pos.ToString() + " : " + length.ToString());
        //    var num = (pos / length) * 100;
        //    MessageBox.Show(num.ToString());
        //    playbackSlider.Value = num;
        //}

        private void playbackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                var value = playbackSlider.Value; //0 : 100
                double length = player.NaturalDuration.TimeSpan.TotalSeconds;
                TimeSpan t = TimeSpan.FromSeconds(value * length / 100);
                player.Position = t;
            }
            catch
            {

            }
        }

        private void addNewPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            isCreatingPlaylist = true;
            player.Close();

            if (playlistNameWindow.IsOpen != true)
            {
                playlistNameWindow.IsOpen = true;
                playlistPickerComboBox.IsEnabled = false;
            }
            else
            {
                playlistPickerComboBox.IsEnabled = true;
                playlistNameWindow.IsOpen = false;
            }
        }

        private void cancelNewPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistNameWindow.IsOpen = false;
            playlistPreviewPopUp.IsOpen = false;
            isCreatingPlaylist = false;
            viewCurrentPlaylistBtn.Visibility = Visibility.Hidden;
            finishNewPlaylistBtn.Visibility = Visibility.Hidden;
            cancelNewPlaylistBtn.Visibility = Visibility.Hidden;
            playlistIDs = "";
            playlistName = "";
            playlistPickerComboBox.IsEnabled = true;
        }

        private void finishNewPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
   
                isCreatingPlaylist = false;

            
            if (!String.IsNullOrWhiteSpace(playlistIDs))
            {
                isCreatingPlaylist = false;
                MessageBox.Show(playlistName + " " + playlistIDs);
                var insertPlaylistQuery = "INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES(@playlistName, @songIDs)";
                //INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES('@playlistName', '@PlaylistIDs')
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand insert = new SqlCommand(insertPlaylistQuery, connection);
                    insert.Parameters.AddWithValue("@playlistName", playlistName);
                    insert.Parameters.AddWithValue("@songIDs", playlistIDs);
                    finishNewPlaylistBtn.Visibility = Visibility.Hidden;
                    cancelNewPlaylistBtn.Visibility = Visibility.Hidden;
                    viewCurrentPlaylistBtn.Visibility = Visibility.Hidden;
                    playlistPreviewPopUp.IsOpen = false;

                    insert.ExecuteNonQuery();
                    connection.Close();
                    playlistPickerComboBox.Items.Add(playlistName);
                    playlistPickerComboBox.IsEnabled = true;
                    playlistName = "";
                    playlistIDs = "";
                }
            }
            else
            {
                MessageBox.Show("Pick some songs");
            }
        }



        private void songsDataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null)
            {

            }
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                DataGridRow row = dep as DataGridRow;
                TextBlock songName = songsDataGrid.Columns[0].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;
                TextBlock artist = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;
                TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;


                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    if (row != null)
                    {
                        //try
                        //{
                            tableIndex = row.GetIndex();//Get clicked index
                                                        //MessageBox.Show(tableIndex.ToString());

                            currentSongID = Int32.Parse(id.Text);
                            if (isCreatingPlaylist != true)
                            {
                                string dir = directoryReturner(Int32.Parse(id.Text));
                                if (File.Exists(dir))
                                {
                                    player.Open(new Uri(dir));
                                    player.Volume = volumeSlider.Value / 100;
                                    musicPlaying = true;
                                    songSelected = true;
                                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                                    currentSongID = Int32.Parse(id.Text);
                                    songsDataGrid.SelectedItems.Clear();
                                    updateCurrentlyPlayingLabel(tableIndex);
                                    playbackSlider.Value = 0;
                                    player.Play();
                                }
                                else
                                {
                                    using (SqlConnection connection = new SqlConnection(connectionString))
                                    {
                                        connection.Open();
                                        var filename = Path.GetFileName(dir);
                                        MessageBox.Show("File does not exist " + filename); //Delete file from db
                                        SqlCommand remove = connection.CreateCommand();
                                        remove.CommandText = "DELETE FROM Songs WHERE Directory = @dir";
                                        remove.Parameters.AddWithValue("@dir", filename);
                                        remove.ExecuteNonQuery();
                                        MessageBox.Show("Removed from DB");
                                        PopulateSongs();
                                    }
                                }


                            }
                            else if (isCreatingPlaylist == true)
                            {
                                


                                playlistViewer.Rows.Add(songName.Text, artist.Text, id.Text);
                                    playlistSongPreviewerDataGrid.ItemsSource = playlistViewer.DefaultView;
                                    playlistIDs += id.Text + ",";
                                



                            }
                        //}
                        //catch
                        //{

                        //}
                    }
                }
                else
                {

                    if (isCreatingPlaylist == false)
                    {
                        if (rightClickMenu.IsOpen == false)
                        {
                            rightClickMenu.IsOpen = true;
                            rightClickedSongID = Int32.Parse(id.Text);

                        }
                        else
                        {
                            rightClickMenu.IsOpen = false;
                            isCreatingPlaylist = false;
                            rightClickedSongID = 0;

                        }
                    }
                }


            }
        }



        private void addToPlaylistMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            isCreatingPlaylist = true;
            loadPlaylistNames(addToPlaylistNamesComboBox);
            if (addToPlaylistPrompt.IsOpen == false)
            {
                rightClickMenu.IsOpen = false;
                addToPlaylistPrompt.IsOpen = true;
            }
            else
            {
                addToPlaylistPrompt.IsOpen = false;
            }
        }


        private void loadPlaylistNames(ComboBox comboBox)
        {
            var query = "SELECT PlaylistName FROM Playlist";

            List<String> nameData = new List<String>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var str = reader.GetString(0);
                            if (str != null)
                            {
                                nameData.Add(str);

                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            for (var i = 0; i < nameData.Count; i++)
            {
                comboBox.Items.Add(nameData[i]);
            }
        }

        private void cancelAddToPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            rightClickedSongID = 0;
            addToPlaylistPrompt.IsOpen = false;
            isCreatingPlaylist = false;
        }

        private void saveToPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            var item = addToPlaylistNamesComboBox.SelectedValue;

            if (!string.IsNullOrWhiteSpace(item.ToString()))
            {
                var selectPlaylistIDsQuery = "SELECT [Song ID] FROM Playlist WHERE PlaylistName = @playlistName";
                //INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES('@playlistName', '@PlaylistIDs')
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand select = new SqlCommand(selectPlaylistIDsQuery, connection);
                    select.Parameters.AddWithValue("@playlistName", item);
                    var IDs = select.ExecuteScalar() + "," + rightClickedSongID;
                    var insertNewSong = "UPDATE Playlist SET [Song ID] = @IDs WHERE PlaylistName = @playlistName";
                    SqlCommand insertSong = new SqlCommand(insertNewSong, connection);
                    insertSong.Parameters.AddWithValue("@IDs", IDs);
                    insertSong.Parameters.AddWithValue("@playlistName", item);
                    insertSong.ExecuteNonQuery();
                    connection.Close();
                    playlistPickerComboBox.IsEnabled = true;
                    isCreatingPlaylist = false;
                    rightClickedSongID = 0;
                    addToPlaylistPrompt.IsOpen = false;
                    updateDataGrid();

                }
            }
            else
            {
                MessageBox.Show("Please choose a playlist to add to");
            }


        }

        private void deleteSongEntirelyMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            var deleteSong = "DELETE FROM Songs WHERE Id = @ID ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand delete = new SqlCommand(deleteSong, connection);
                delete.Parameters.AddWithValue("@ID", rightClickedSongID);
                delete.ExecuteNonQuery();
                rightClickMenu.IsOpen = false;
                connection.Close();
                updateDataGrid();

                ///Delete actual file too

            }
        }

        private void deleteFromPlaylistMenuBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchInfo = searchBox.Text;
            if (searchBox.Text == "")
            {



                playlistPickerComboBox.SelectedValue = "All Songs";
                PopulateSongs();
                playlistPickerComboBox.IsEnabled = true;
                searchPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                playlistPickerComboBox.SelectedValue = "All Songs";
                playlistPickerComboBox.IsEnabled = false;
                searchPlaceholder.Visibility = Visibility.Hidden;
                var query = "SELECT * FROM Songs WHERE Name LIKE @info";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    var ad = new SqlDataAdapter(query, connection);
                    ad.SelectCommand.Parameters.AddWithValue("@info", "%" + searchInfo + "%");
                    DataTable songTable = new DataTable();
                    ad.Fill(songTable);
                    songsDataGrid.ItemsSource = songTable.DefaultView;




                }
            }
        }

        private void viewCurrentPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            if(playlistPreviewPopUp.IsOpen == false) {
                playlistPreviewPopUp.IsOpen = true;
            }
            else
            {
                playlistPreviewPopUp.IsOpen = false;
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Popup myPopup = playlistPreviewPopUp;
            Window w = Window.GetWindow(MainWin);
            if (null != w)
            {

                    var offset = myPopup.HorizontalOffset;
                    myPopup.HorizontalOffset = offset + 1;
                    myPopup.HorizontalOffset = offset;
                };
            }

        private void MainWin_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Popup myPopup = playlistPreviewPopUp;
            Window w = Window.GetWindow(MainWin);
            if (null != w)
            {

                myPopup.Width = w.Width;
            };
        }
    }
    
    }

   

