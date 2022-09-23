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
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SynopticMusicPlayer
{

    public partial class MainWindow : Window
    {
        SqlConnection connection;
        string connectionString;
        MediaPlayer player;
        DataTable playlistViewer;
        bool musicPlaying;
        bool songSelected;
        bool isCreatingPlaylist;
        int currentPlayingSongDbID;
        int rightClickedSongID;
        int rightClickedTableIndex;
        int currentTableIndex;
        string playlistName;
        string currentAlbum;
        string playlistIDs = "";
        string folderLocation;
        int faqMenuIndex;
        DispatcherTimer timer;
        public delegate void timerTick();
        timerTick tick;
        bool isDragging;

        public MainWindow()
        {
            connectionString = ConfigurationManager.ConnectionStrings["SynopticMusicPlayer.Properties.Settings.SongsConnectionString"].ConnectionString;
            InitializeComponent();
            checkForFirstRun();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(timer_Tick);
            tick = new timerTick(changeStatus);

        }


        private void PopulateSongs() //Populate DataGrid with ALL SONGS from database
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs", connection))
            {
                DataTable songTable = new DataTable();
                adapter.Fill(songTable);
                songsDataGrid.ItemsSource = songTable.DefaultView;
            }
            if(songsDataGrid.Items.Count == 0)
            {
                noSongsFoundLabel.Content = "No songs found in: " + folderLocation;
                noSongsFoundLabel.Visibility = Visibility.Visible;
            }
            else
            {
                noSongsFoundLabel.Visibility = Visibility.Hidden;
            }
        }

        private void checkForFirstRun()
        {
            openBrowserDialogTextBox.Text = Properties.Settings.Default.MusicDirectory;
            browserDialog.IsOpen = true;
        }

        private string directoryReturner(int index) //Return the directory of a file using its database ID
        {
            var query = "SELECT Directory FROM Songs WHERE Id = @index";
            using (connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@index", index);
                string dir = (string)command.ExecuteScalar();
                connection.Close();
                dir = folderLocation + @"\" + dir;
                return dir;
            }
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)//Event for volume changed
        {
            player.Volume = volumeSlider.Value / 100;
        }

        private void setUp() //Set up func
        {
            checkForUpdates();
            playlistPickerComboBox.Items.Add("All Songs");
            loadPlaylistNames(playlistPickerComboBox);
            loadAlbumNames(playlistPickerComboBox);
            player = new MediaPlayer();
            player.Volume = 0;
            volumeSlider.Value = 25;
            player.MediaEnded += songFinished;
            player.MediaOpened += songOpened;
        }

        private void songFinished(object sender, EventArgs e)//Plays the next song when a song finishes
        {
            if ((currentTableIndex + 1) < songsDataGrid.Items.Count && songSelected == true)
            {
                currentTableIndex++;
                TextBlock id = songsDataGrid.Columns[5].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                currentPlayingSongDbID = Int32.Parse(id.Text);
                string dir = directoryReturner(Int32.Parse(id.Text));
                if (dir != null)
                {
                    startSong(dir);
                }
                else
                {
                    MessageBox.Show("File not found");
                }
            }
            else
            {
                MessageBox.Show("No more music");
            }
        }

        private void songOpened(object sender, EventArgs e)//Sets values and icons when a new song is opened
        {
            player.Volume = volumeSlider.Value / 100;
            musicPlaying = true;
            songSelected = true;
            playbackSlider.Value = 0;
            playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
            updateCurrentlyPlayingLabel(currentTableIndex);
            songsDataGrid.SelectedItems.Clear();
        }

        private void startSong(string dir)
        {
            player.Open(new Uri(dir));
            player.Play();
            timer.Start();
        }
        void timer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(tick);
        }

        void changeStatus()
        {
            if (musicPlaying == true)
            {
                playbackSlider.Value = player.Position.TotalSeconds;
                try
                {
                    playbackSlider.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
                }
                catch
                {
                    Console.WriteLine("Timespan Exception");
                }
            }
        }

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
                        player.Stop();
                        musicPlaying = false;
                        updateCurrentlyPlayingLabel(0);
                        playlistNameWindow.IsOpen = false;
                        finishNewPlaylistBtn.Visibility = Visibility.Visible;
                        viewCurrentPlaylistBtn.Visibility = Visibility.Visible;
                        cancelNewPlaylistBtn.Visibility = Visibility.Visible;
                        isCreatingPlaylist = true;
                        playlistViewer = new DataTable();
                        playlistViewer.Columns.Add("Name");
                        playlistViewer.Columns.Add("Artist");
                        playlistViewer.Columns.Add("Album");
                        playlistViewer.Columns.Add("Length");
                        playlistViewer.Columns.Add("ID");
                        playlistPreviewPopUp.IsOpen = true;
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

        private int getAlbumIndex()
        {
            int AlbumItemIndex;
            for (int i = 0; i < playlistPickerComboBox.Items.Count; i++)
            {
                if ((string)playlistPickerComboBox.Items[i] == "--Albums--")
                {
                    AlbumItemIndex = i;
                    return AlbumItemIndex;
                }
            }
            return 0;
        }

        private void updateDataGrid()
        {
            var selection = playlistPickerComboBox.SelectedValue;
            if (selection.ToString() != "All Songs" && selection.ToString() != "--Albums--")
            {
                if (playlistPickerComboBox.SelectedIndex < getAlbumIndex()) //Selected a playlist
                {
                    var query = "SELECT [Song ID] FROM Playlist WHERE PlaylistName='" + selection + "'";
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
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        var ad = new SqlDataAdapter("SELECT * FROM Songs WHERE Album = @album", connection);
                        ad.SelectCommand.Parameters.AddWithValue("@album", selection);
                        DataTable songTable = new DataTable();
                        ad.Fill(songTable);
                        songsDataGrid.ItemsSource = songTable.DefaultView;
                        currentAlbum = selection.ToString();
                        playlistPickerComboBox.SelectedValue = (selection);
                        musicPlaying = false;
                        updateCurrentlyPlayingLabel(0);
                        songSelected = false;
                        playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                        player.Close();
                    }
                }
            }
            else
            {
                playlistPickerComboBox.SelectedValue = "All Songs";
                PopulateSongs();
            }
        }

        private void checkForUpdates()
        {
            int fileCount = Directory.GetFiles(folderLocation, "*.*", SearchOption.AllDirectories).Length;
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
                string[] fileEntries = Directory.GetFiles(folderLocation);
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
                            var songAlbum = file.Tag.Album;
                            var songLength = file.Properties.Duration.Minutes + ":" + file.Properties.Duration.Seconds;
                            if (songTitle == null)//blank title use file name
                            {
                                songTitle = filename;
                            }
                            if (songAlbum == null)
                            {
                                songAlbum = "No Album";
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
                            addSong.Parameters.AddWithValue("@album", songAlbum);
                            addSong.Parameters.AddWithValue("@length", songLength);
                            try
                            {
                                addSong.CommandText = "INSERT INTO Songs(Name, Artist, Directory, Album, Length) VALUES(@name, @artist, @dir, @album, @length)";
                                addSong.ExecuteNonQuery();
                            }
                            catch
                            {
                                MessageBox.Show("Failed to insert data");
                            }
                        }
                    }

                }
            }
        }
        private void updateCurrentlyPlayingLabel(int currentTableIndex)
        {

            if (musicPlaying == true)
            {
                TextBlock name = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                TextBlock artist = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                currentlyPlayingLabel.Content = name.Text + " - " + artist.Text;
            }
            else
            {
                currentlyPlayingLabel.Content = "Nothing";

            }
        }



        private void shuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (songsDataGrid.Items.Count != 0 && isCreatingPlaylist == false)
            {
                Random rnd = new Random();
                int num = rnd.Next(0, songsDataGrid.Items.Count);
                TextBlock id = songsDataGrid.Columns[5].GetCellContent(songsDataGrid.Items[num]) as TextBlock;//Save Song ID from selected row index
                currentTableIndex = num;
                var dir = directoryReturner(Int32.Parse(id.Text));
                currentPlayingSongDbID = Int32.Parse(id.Text);
                player.Open(new Uri(dir));
                player.Play();
            }
        }



        private void addNewPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {


            if (playlistNameWindow.IsOpen != true)
            {
                playlistNameWindow.IsOpen = true;
            }
            else
            {
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
            if (!String.IsNullOrWhiteSpace(playlistIDs))
            {
                isCreatingPlaylist = false;
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
                    playlistPickerComboBox.IsEnabled = true;
                    playlistName = "";
                    playlistIDs = "";
                    playlistPickerComboBox.Items.Clear();
                    playlistPickerComboBox.Items.Add("All Songs");
                    loadPlaylistNames(playlistPickerComboBox);
                    loadAlbumNames(playlistPickerComboBox);
                    playlistPickerComboBox.SelectedValue = "All Songs";
                }
            }
            else
            {
                MessageBox.Show("Pick some songs");
            }
        }

        private DataGridRow getClickedRow(MouseButtonEventArgs e)
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
            }
            return dep as DataGridRow;
        }

        private void songsDataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = getClickedRow(e);

            if (row == null)
            {
                return;
            }

            TextBlock songName = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;
            TextBlock artist = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;
            TextBlock album = songsDataGrid.Columns[3].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;
            TextBlock length = songsDataGrid.Columns[4].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;

            TextBlock id = songsDataGrid.Columns[5].GetCellContent(songsDataGrid.Items[row.GetIndex()]) as TextBlock;

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (row != null)
                {

                    currentTableIndex = row.GetIndex();//Get clicked index
                    currentPlayingSongDbID = Int32.Parse(id.Text); // set current song ID

                    if (isCreatingPlaylist != true)
                    {
                        string dir = directoryReturner(currentPlayingSongDbID);
                        if (File.Exists(dir))
                        {
                            startSong(dir);

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
                        playlistViewer.Rows.Add(songName.Text, artist.Text, album.Text, length.Text, id.Text);
                        playlistSongPreviewerDataGrid.ItemsSource = playlistViewer.DefaultView;
                        playlistIDs += id.Text + ",";
                    }

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
                        songsDataGrid.SelectedItem = songsDataGrid.Items[row.GetIndex()];
                        rightClickedTableIndex = row.GetIndex();
                    }
                    else
                    {
                        isCreatingPlaylist = false;
                    }
                }
            }
        }

        private void addToPlaylistMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            addToPlaylistNamesComboBox.Items.Clear();
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

        private void loadAlbumNames(ComboBox comboBox)
        {
            comboBox.Items.Add("--Albums--");
            var query = "SELECT Album FROM Songs";

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
                            try
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
                            catch
                            {

                            }
                        }
                    }
                }
            }
            for (var i = 0; i < nameData.Count; i++)
            {
                if (nameData[i] != null)
                {
                    if (!comboBox.Items.Contains(nameData[i]))
                    {
                        comboBox.Items.Add(nameData[i]);
                    }
                }
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
            if (item != null && !string.IsNullOrWhiteSpace(item.ToString()))
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
                var dir = directoryReturner(rightClickedSongID);
                File.Delete(dir);
                connection.Open();
                SqlCommand delete = new SqlCommand(deleteSong, connection);
                delete.Parameters.AddWithValue("@ID", rightClickedSongID);
                delete.ExecuteNonQuery();
                rightClickMenu.IsOpen = false;
                connection.Close();
                rightClickMenu.IsOpen = false;
                updateDataGrid();
            }
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
            if (playlistPreviewPopUp.IsOpen == false)
            {
                playlistPreviewPopUp.IsOpen = true;
            }
            else
            {
                playlistPreviewPopUp.IsOpen = false;
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Popup playlistPreviewWindow = playlistPreviewPopUp;
            Popup faqWindow = helpFAQmenu;
            Popup helpWindowTemplate = helpMenuTemplate;
            Window w = Window.GetWindow(MainWin);
            if (null != w)
            {
                var offset = playlistPreviewPopUp.HorizontalOffset;
                playlistPreviewWindow.HorizontalOffset = offset + 1;
                playlistPreviewWindow.HorizontalOffset = offset;
                faqWindow.HorizontalOffset = offset + 1;
                faqWindow.HorizontalOffset = offset;
                helpWindowTemplate.HorizontalOffset = offset + 1;
                helpWindowTemplate.HorizontalOffset = offset;
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

        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            helpFAQmenu.IsOpen = true;
        }

        private void faqStackPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock)
            {
                var labelClicked = e.OriginalSource as TextBlock;

                if ((string)labelClicked.DataContext == ">What is this?")
                {
                    helpFAQmenu.IsOpen = false;
                    helpMenuTemplate.IsOpen = true;
                    helpMenuTitle.Text = uiText.faqTitle1;
                    helpMenuTextBlock.Text = uiText.faqText1;
                }
                else if ((string)labelClicked.DataContext == ">How do I play my music?")
                {
                    helpFAQmenu.IsOpen = false;
                    helpMenuTemplate.IsOpen = true;
                    helpMenuTitle.Text = uiText.faqTitle2;
                    helpMenuTextBlock.Text = uiText.faqText2;
                }
                else if ((string)labelClicked.DataContext == ">How can I add songs?")
                {
                    helpFAQmenu.IsOpen = false;
                    helpMenuTemplate.IsOpen = true;
                    helpMenuTitle.Text = uiText.faqTitle3;
                    helpMenuTextBlock.Text = uiText.faqText3;
                }
                else if ((string)labelClicked.DataContext == ">How can I create playlists?")
                {
                    faqMenuIndex = 0;
                    helpFAQmenu.IsOpen = false;
                    helpMenuBackBtn.Visibility = Visibility.Visible;
                    helpMenuNextBtn.Visibility = Visibility.Visible;
                    helpMenuTemplate.IsOpen = true;
                    helpMenuTextBlock.Text = uiText.faqText41;
                    helpMenuImage.Visibility = Visibility.Visible;
                    helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage4.png")));
                    helpMenuTitle.Text = uiText.faqTitle4;
                }
                else if ((string)labelClicked.DataContext == ">How can I change playlists?")
                {
                    helpFAQmenu.IsOpen = false;
                    helpMenuTemplate.IsOpen = true;
                    helpMenuTitle.Text = uiText.faqTitle5;
                    helpMenuTextBlock.Text = uiText.faqText5;
                    helpMenuImage.Visibility = Visibility.Visible;
                    helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage1.png")));
                }
                else if ((string)labelClicked.DataContext == ">I've found a bug, what should I do?")
                {
                    helpFAQmenu.IsOpen = false;
                    helpMenuTemplate.IsOpen = true;
                    helpMenuTitle.Text = uiText.faqTitle6;
                    helpMenuTextBlock.Text = uiText.faqText6;
                }
            }
        }

        private void helpMenuReturnBtn_Click(object sender, RoutedEventArgs e)
        {
            helpMenuTemplate.IsOpen = false;
            helpFAQmenu.IsOpen = true;
            helpMenuBackBtn.Visibility = Visibility.Hidden;
            helpMenuNextBtn.Visibility = Visibility.Hidden;
            helpMenuImage.Visibility = Visibility.Hidden;
        }

        private void helpMenuNextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (faqMenuIndex + 1 <= 2)
            {
                faqMenuIndex++;
            }
            if (faqMenuIndex == 0)
            {
                helpMenuTextBlock.Text = uiText.faqText41;
                helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage4.png")));
            }
            else if (faqMenuIndex == 1)
            {
                helpMenuTextBlock.Text = uiText.faqText42;
                helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage2.png")));
            }
            else if (faqMenuIndex == 2)
            {
                helpMenuTextBlock.Text = uiText.faqText43;
                helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage3.png")));
            }
        }

        private void helpMenuBackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (faqMenuIndex - 1 >= 0)
            {
                faqMenuIndex--;
            }

            if (faqMenuIndex == 0)
            {
                helpMenuTextBlock.Text = uiText.faqText41;
                helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage4.png")));
            }
            else if (faqMenuIndex == 1)
            {
                helpMenuTextBlock.Text = uiText.faqText42;
                helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage2.png")));
            }
            else if (faqMenuIndex == 2)
            {
                helpMenuTextBlock.Text = uiText.faqText43;
                helpMenuImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Images\helpMenuImage3.png")));
            }
        }

        private void playbackSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TimeSpan ts = new TimeSpan(0, 0, 0, (int)playbackSlider.Value, 0);
            changePosition(ts);
        }

        private void playbackSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("dragging");
            isDragging = true;
        }

        private void playbackSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                TimeSpan ts = new TimeSpan(0, 0, 0, (int)playbackSlider.Value, 0);
                changePosition(ts);
            }
            isDragging = false;
        }
        void changePosition(TimeSpan ts)
        {
            player.Position = ts;
        }

        private void filterByAlbumBtn_Click(object sender, RoutedEventArgs e)
        {
            TextBlock albumName = songsDataGrid.Columns[3].GetCellContent(songsDataGrid.Items[rightClickedTableIndex]) as TextBlock;
            musicPlaying = false;
            updateCurrentlyPlayingLabel(0);
            songSelected = false;
            playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
            player.Stop();

            if (albumName.Text != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    var ad = new SqlDataAdapter("SELECT * FROM Songs WHERE Album = @album", connection);
                    ad.SelectCommand.Parameters.AddWithValue("@album", albumName.Text);
                    DataTable songTable = new DataTable();
                    ad.Fill(songTable);
                    songsDataGrid.ItemsSource = songTable.DefaultView;
                    currentAlbum = albumName.Text;
                    playlistPickerComboBox.SelectedValue = (albumName.Text);
                    rightClickMenu.IsOpen = false;
                }
            }
            else
            {
                MessageBox.Show("This song is not apart of an album");
            }
        }

        private void deleteSongFromPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            if (playlistPickerComboBox.SelectedIndex != 0 && playlistPickerComboBox.SelectedIndex < getAlbumIndex())
            {
                var playlistName = playlistPickerComboBox.SelectedValue;
                var query = "SELECT [Song ID] FROM Playlist WHERE PlaylistName = @playlistName";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand retrieveIDs = new SqlCommand(query, connection))
                    {
                        retrieveIDs.Parameters.AddWithValue("@playlistName", playlistName);
                        using (SqlDataReader reader = retrieveIDs.ExecuteReader())
                        {
                            reader.Read();

                            var str = reader.GetString(0);
                            if (str != null)
                            {

                                List<String> IDs = str.Split(',').ToList();
                                IDs.RemoveAt(rightClickedTableIndex);
                                string newID = string.Join(",", IDs);
                                reader.Close();
                                using (SqlCommand insertIDs = new SqlCommand(query, connection))
                                {
                                    insertIDs.CommandText = "UPDATE Playlist SET [Song ID] = @newIds WHERE PlaylistName = @PlaylistName";
                                    insertIDs.Parameters.AddWithValue("@newIDs", newID);
                                    insertIDs.Parameters.AddWithValue("@PlaylistName", playlistName.ToString());
                                    insertIDs.ExecuteNonQuery();
                                    updateDataGrid();
                                    rightClickMenu.IsOpen = false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("You are not within a playlist");
            }
        }

        private void nextSongBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((currentTableIndex + 1) < songsDataGrid.Items.Count && songSelected == true)
            {
                try
                {
                    currentTableIndex++;
                    TextBlock id = songsDataGrid.Columns[5].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                    currentPlayingSongDbID = Int32.Parse(id.Text);
                    string dir = directoryReturner(Int32.Parse(id.Text));
                    //MessageBox.Show(id.Text); //outputs 2

                    if (dir != null && isCreatingPlaylist == false)
                    {
                        startSong(dir);
                    }
                }
                catch
                {
                    currentTableIndex = 0;
                }
            }
            else if(songSelected == false)
            {
                MessageBox.Show("No song selected");
            }
            else
            {
                MessageBox.Show("No more music");
            }
        }

        private void playSongBtn_Click(object sender, RoutedEventArgs e)
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

        private void prevSongBtn_Click(object sender, RoutedEventArgs e)
        {
            if ((currentTableIndex - 1) >= 0 && songSelected == true)
            {
                try
                {
                    currentTableIndex--;
                    TextBlock id = songsDataGrid.Columns[5].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                    currentPlayingSongDbID = Int32.Parse(id.Text);
                    string dir = directoryReturner(Int32.Parse(id.Text));
                    if (dir != null && isCreatingPlaylist == false)
                    {
                        startSong(dir);
                    }

                }
                catch
                {
                    currentTableIndex = 0;
                }
            }
            else if (songSelected == false)
            {
                MessageBox.Show("No song selected");
            }
            else
            {
                MessageBox.Show("No more music");
            }
        }

        private void openBrowserDialogBtn_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var dir = dialog.FileName;
                openBrowserDialogTextBox.Text = dialog.FileName;
            }
        }

        private void openBrowserDialogSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (openBrowserDialogTextBox.Text != null && !string.IsNullOrWhiteSpace(openBrowserDialogTextBox.Text))
            {
                string dir = openBrowserDialogTextBox.Text + @"\Music";

                if (Path.GetFileName(openBrowserDialogTextBox.Text) == "Music")
                {
                    MessageBox.Show("Music folder found");
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.MusicDirectory = openBrowserDialogTextBox.Text;
                    folderLocation = Properties.Settings.Default.MusicDirectory;
                    browserDialog.IsOpen = false;
                    Properties.Settings.Default.FirstRun = false;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show("New folder has been created");
                    System.IO.Directory.CreateDirectory(dir);
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.MusicDirectory = dir;
                    folderLocation = Properties.Settings.Default.MusicDirectory;
                    browserDialog.IsOpen = false;
                    Properties.Settings.Default.FirstRun = false;
                    Properties.Settings.Default.Save();
                }

                setUp();
                PopulateSongs();
            }
            else
            {
                MessageBox.Show("Invalid Directory");
            }
        }

        private void openBrowserDialogQuitBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}



