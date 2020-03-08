using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace _1712907_1712912_1712914
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BindingList<PlayList> mPlayListArray = new BindingList<PlayList>();
        public static MediaPlayer mMediaPlayer = new MediaPlayer();
        DispatcherTimer mDispatcherTimer = new DispatcherTimer();
        //List<int> mRemoveFileArray = new List<int>();
        bool mIsPlaying = false;
        bool mIsPlayedFromFirst = true;
        int mLastIndex = 0;
        Brush mPlayingFileNameTextColor = Brushes.Green;
        Brush mNotPlayingFileNameTextColor = Brushes.Black;
        List<int> mSequentialPlayListIndexArray = new List<int>();
        Random rand = new Random();
        private IKeyboardMouseEvents mHook;

        RotateTransform transform;
        DoubleAnimation animation;
        double currentAngle = 0;



        public MainWindow()
        {
            InitializeComponent();
            //TimerSlider.DataContext = mMediaPlayer.Position;
            SoundSlider.DataContext = mMediaPlayer;
            MuteCheckBox.DataContext = mMediaPlayer;

            TimerSliderLabel timerSliderLabel = new TimerSliderLabel();
            Binding myBindingTimerLabel = new Binding("LabelString");
            myBindingTimerLabel.Source = timerSliderLabel;
            TimerLabel.SetBinding(Label.ContentProperty, myBindingTimerLabel);

            Binding myBindingTimerSlider = new Binding("SliderDouble");
            myBindingTimerSlider.Source = timerSliderLabel;
            TimerSlider.SetBinding(Slider.ValueProperty, myBindingTimerSlider);

            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = new BitmapImage(new Uri("example.jpg", UriKind.Relative));
            CoverImage.Fill = imageBrush;

            transform = new RotateTransform()
            {
                CenterX = 0.5,
                CenterY = 0.5,
                Angle = 0,
            };

            CoverImage.RenderTransform = transform;

            animation = new DoubleAnimation()
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(10),
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlaylistListBox.ItemsSource = mPlayListArray;
            CheckListBox.ItemsSource = mPlayListArray;

            mMediaPlayer.MediaEnded += mMediaPlayer_MediaEnded;

            mDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            mDispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            mDispatcherTimer.Interval = TimeSpan.FromMilliseconds(500);

            // Dang ky su kien hook
            mHook = Hook.GlobalEvents();
            mHook.KeyUp += KeyUp_Hook;

            try
            {
                //nạp playlist gần nhất
                const string fileName = "current_playlist.txt";
                using (var reader = new StreamReader(fileName))
                {
                    var data = reader.ReadToEnd();
                    char[] delim = { '\n' };
                    string[] playlist = data.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    int playlistLength = int.Parse(playlist[0]);
                    for (int i = 1; i <= playlistLength; i++)
                    {
                        var selectedFile = new FileInfo(playlist[i]);
                        mPlayListArray.Add(new PlayList(selectedFile, false, mNotPlayingFileNameTextColor));
                    }
                    reader.Close();
                }
            }
            catch (Exception)
            {
                return;
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            mHook.KeyUp -= KeyUp_Hook;
            mHook.Dispose();

            //lưu playlist gần nhất
            const string fileName = "current_playlist.txt";
            try
            {
                var writer = new StreamWriter(fileName);

                string playlist = AssignCurrentPlaylistToString();
                writer.Write(playlist);
                writer.Close();
            }
            catch (Exception err)
            {

            }
        }

        private void initState()
        {
            mIsPlaying = false;
            mIsPlayedFromFirst = true;
            mDispatcherTimer.Stop();
            mMediaPlayer.Stop();
            PlayPauseButtonImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "Images/play-button.png"));
        }

        private int getNextIndexFromPlayListArray(int index)
        {
            if (index < mPlayListArray.Count - 1)
            {
                return index + 1;
            }
            else
            {
                return 0;
            }
        }
        private int getLastIndexFromPlayListArray(int index)
        {
            if (index > 0)
            {
                return index - 1;
            }
            else
            {
                return mPlayListArray.Count - 1;
            }
        }

        private void mMediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            StopAnimation();

            updateTimer();
            initState();
            mPlayListArray[mLastIndex].TextColor = mNotPlayingFileNameTextColor;

            //Lặp 1 bài vô tận
            if (OneMusicLoopModeToggleButton.IsChecked == true)
            {
                PlaySelectedIndex(mLastIndex);
                return;
            }

            if (SequentialPlayingRadioButton.IsChecked == true)
            {
                // Lặp cả playlist 1 lần, tuần tự
                if (AllPlayListLoopModeToggleButton.IsChecked == false)
                {
                    mSequentialPlayListIndexArray.Remove(mLastIndex);
                    if (mSequentialPlayListIndexArray.Count == 0)
                    {
                        MessageBox.Show("Đã nghe hết playlist");
                        SequentialPlayingRadioButton.IsChecked = false;
                        return;
                    }
                    while (true)
                    {
                        mLastIndex = getNextIndexFromPlayListArray(mLastIndex);

                        if (mSequentialPlayListIndexArray.Contains(mLastIndex))
                        {
                            PlaySelectedIndex(mLastIndex);
                            return;
                        }
                    }
                }
                // Lặp cả playlist vô tận, tuần tự
                else
                {
                    mLastIndex = getNextIndexFromPlayListArray(mLastIndex);
                    PlaySelectedIndex(mLastIndex);
                    return;
                }
            }

            //Lặp cả playlist ngẫu nhiên
            else if (RandomPlayingRadioButton.IsChecked == true)
            {
                mLastIndex = rand.Next(mPlayListArray.Count);
                PlaySelectedIndex(mLastIndex);
            }
        }

        private void FileMode_AddButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Audio Files (.)|*.aif;*.cda;*.mid;*.midi;*.mp3;*.mpa;*.ogg;*.wav;*.wma;*.wpl;*.mp4";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var item in openFileDialog.FileNames)
                {
                    var selectedFile = new FileInfo(item);
                    mPlayListArray.Add(new PlayList(selectedFile, false, mNotPlayingFileNameTextColor));

                }
            }
        }

        private void FolderMode_AddButton_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();


            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<string> audioFileExtensionList = new List<string>() { ".aif", ".cda", ".mid", ".midi", ".mp3", ".mpa", ".ogg", ".wav", ".wma", ".wpl", ".mp4" };
                string[] pathFiles = Directory.GetFiles(folderBrowserDialog.SelectedPath);
                foreach (var dir in pathFiles)
                {
                    var selectedFile = new FileInfo(dir);
                    if (audioFileExtensionList.Contains(selectedFile.Extension))
                    {
                        mPlayListArray.Add(new PlayList(selectedFile, false, mNotPlayingFileNameTextColor));
                    }
                }
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!mIsPlaying)
            {
                if (mPlayListArray.Count > 0)
                {
                    PlaySelectedIndex(mLastIndex);
                }
                else
                {
                    System.Windows.MessageBox.Show("Chưa chọn file");
                }
            }
            else
            {
                StopAnimation();

                mDispatcherTimer.Stop();
                mMediaPlayer.Pause();
                PlayPauseButtonImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "Images/play-button.png"));
                mIsPlaying = false;
            }
        }

        private void PlaySelectedIndex(int i)
        {
            StartAnimation();

            mIsPlaying = true;
            PlayPauseButtonImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "Images/pause-button.png"));
            mPlayListArray[i].TextColor = mPlayingFileNameTextColor;
            if (mIsPlayedFromFirst)
            {
                mMediaPlayer.Open(new Uri(mPlayListArray[i].FullName, UriKind.Absolute));
                System.Threading.Thread.Sleep(1000); // ngủ để cho nó kịp open thì mới có timeSpan
                //if (!mMediaPlayer.NaturalDuration.HasTimeSpan)
                //{
                //    mPlayListArray[i].TextColor = mNotPlayingFileNameTextColor;
                //    initState();
                //    MessageBox.Show("File " + mPlayListArray[i].Name + " không phải là file nhạc");
                //    return;
                //}
                mMediaPlayer.Position = TimeSpan.Zero;
                mIsPlayedFromFirst = false;
                mMediaPlayer.Play();
                updateTimer();
                mDispatcherTimer.Start();
            }
            else
            {
                mMediaPlayer.Play();
                updateTimer();
                mDispatcherTimer.Start();
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (mMediaPlayer.Source != null && mIsPlaying)
            {
                //if (!mMediaPlayer.NaturalDuration.HasTimeSpan)
                //    return;
                //mMediaPlayer.Position.Add(new TimeSpan(0, 0, 1)); // thêm 1 giây
                updateTimer();
            }
        }

        private void updateTimer()
        {
            if (mMediaPlayer.Source == null)
                return;
            var currentPos = mMediaPlayer.Position;
            var endPos = mMediaPlayer.NaturalDuration.TimeSpan;
            TimerSlider.Value = (double)Math.Floor(currentPos.TotalMilliseconds) / Math.Floor(endPos.TotalMilliseconds) * TimerSlider.Maximum;
            //TimerLabel.Content = String.Format(currentPos.ToString(@"mm\:ss") + " / " + endPos.ToString(@"mm\:ss"));
        }



        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (mPlayListArray.Count == 0)
                return;
            StopAnimation();
            initState();
            updateTimer();
            mPlayListArray[mLastIndex].TextColor = mNotPlayingFileNameTextColor;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            bool isCheckedFile = false;
            bool isRemovedPlayingFile = false;
            for (int i = 0; i < mPlayListArray.Count; i++)
            {
                if (mPlayListArray[i].IsRemoved)
                {
                    if (i == mLastIndex)
                    {
                        StopAnimation();

                        mLastIndex = 0;
                        initState();
                        isRemovedPlayingFile = true;
                    }
                    mPlayListArray.RemoveAt(i);
                    mSequentialPlayListIndexArray.Remove(i);
                    if (!isRemovedPlayingFile && i < mLastIndex)
                    {
                        mLastIndex--;
                    }
                    i--;
                    isCheckedFile = true;
                }
            }
            if (!isCheckedFile)
            {
                MessageBox.Show("Chưa chọn file");
                return;
            }
            updateTimer();
        }


        private void Playlist_Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                initState();
                mPlayListArray[mLastIndex].TextColor = mNotPlayingFileNameTextColor;
                mLastIndex = PlaylistListBox.SelectedIndex;
                PlaySelectedIndex(mLastIndex);
            }
        }

        private void TimerSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (mMediaPlayer.Source == null)
            {
                TimerSlider.Value = 0;
                return;
            }
            double totalMilliSecondsPlayed = TimerSlider.Value / TimerSlider.Maximum * mMediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            mMediaPlayer.Position = TimeSpan.FromMilliseconds(totalMilliSecondsPlayed);
        }

        private void SequentialPlayingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            mSequentialPlayListIndexArray.Clear();
            for (int i = mLastIndex; i < mPlayListArray.Count; i++)
            {
                mSequentialPlayListIndexArray.Add(i);
            }
            for (int i = 0; i < mLastIndex; i++)
            {
                mSequentialPlayListIndexArray.Add(i);
            }
        }

        private void RandomPlayingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            mSequentialPlayListIndexArray.Clear();
        }

        private void DefaultPlayingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            mSequentialPlayListIndexArray.Clear();
        }

        private void AllPlayListLoopModeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            mSequentialPlayListIndexArray.Clear();
            for (int i = mLastIndex; i < mPlayListArray.Count; i++)
            {
                mSequentialPlayListIndexArray.Add(i);
            }
            for (int i = 0; i < mLastIndex; i++)
            {
                mSequentialPlayListIndexArray.Add(i);
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (mPlayListArray.Count == 0)
                return;
            initState();
            //updateTimer();
            mPlayListArray[mLastIndex].TextColor = mNotPlayingFileNameTextColor;
            mLastIndex = getLastIndexFromPlayListArray(mLastIndex);
            PlaySelectedIndex(mLastIndex);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (mPlayListArray.Count == 0)
                return;
            initState();
            //updateTimer();
            mPlayListArray[mLastIndex].TextColor = mNotPlayingFileNameTextColor;
            mLastIndex = getNextIndexFromPlayListArray(mLastIndex);
            PlaySelectedIndex(mLastIndex);
        }

        //private void TimerSlider_DragOver(object sender, DragEventArgs e)
        //{
        //    var endPos = mMediaPlayer.NaturalDuration.TimeSpan;
        //    var currentPos = TimeSpan.FromSeconds(TimerSlider.Value / TimerSlider.Maximum * Math.Floor(endPos.TotalSeconds));
        //    TimerLabel.Content = String.Format(currentPos.ToString(@"mm\:ss") + " / " + endPos.ToString(@"mm\:ss"));
        //}

        private void KeyUp_Hook(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.P))
            {
                PlayPauseButton_Click(PlayPauseButton, new RoutedEventArgs());
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Escape)
            {
                StopButton_Click(StopButton, new RoutedEventArgs());
            }
            else if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.OemOpenBrackets))
            {
                PreviousButton_Click(PreviousButton, new RoutedEventArgs());
            }
            else if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.OemCloseBrackets))
            {
                NextButton_Click(NextButton, new RoutedEventArgs());
            }
            else if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.S))
            {
                SavePlaylistButton_Click(SavePlaylistButton, new RoutedEventArgs());
            }
            else if (e.Control && (e.KeyCode == System.Windows.Forms.Keys.L))
            {
                LoadPlaylistButton_Click(LoadPlaylistButton, new RoutedEventArgs());
            }
        }

        private string AssignCurrentPlaylistToString()
        {
            string playlist = mPlayListArray.Count.ToString();
            foreach (var item in mPlayListArray)
            {
                playlist = playlist + "\n" + item.FullName;
            }
            return playlist;
        }

        private void SavePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files (.)|*.txt";
            saveFileDialog.FileName = "MyFavouritePlaylist.txt";
            if (saveFileDialog.ShowDialog() == true)
            {
                string playlist = AssignCurrentPlaylistToString();
                File.WriteAllText(saveFileDialog.FileName, playlist);
            }
        }

        private void LoadPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text Files (.)|*.txt";
            if (openFileDialog.ShowDialog() == true)
            {
                initState();
                updateTimer();
                mLastIndex = 0;
                mPlayListArray.Clear();
                mSequentialPlayListIndexArray.Clear();
                OneMusicLoopModeToggleButton.IsChecked = false;
                AllPlayListLoopModeToggleButton.IsChecked = false;
                DefaultPlayingRadioButton.IsChecked = true;
                var playlist = File.ReadAllLines(openFileDialog.FileName);
                int playlistLength = int.Parse(playlist[0]);
                for (int i = 1; i <= playlistLength; i++)
                {
                    var selectedFile = new FileInfo(playlist[i]);
                    mPlayListArray.Add(new PlayList(selectedFile, false, mNotPlayingFileNameTextColor));
                }
            }
        }

        private void StartAnimation()
        {
            animation = new DoubleAnimation()
            {
                From = currentAngle,
                To = 360 + currentAngle,
                Duration = TimeSpan.FromSeconds(10),
                RepeatBehavior = RepeatBehavior.Forever
            };

            transform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }
        private void StopAnimation()
        {
            var rotateTransform = (RotateTransform)CoverImage.RenderTransform;
            rotateTransform.Angle = rotateTransform.Angle;
            currentAngle = rotateTransform.Angle;
            transform.BeginAnimation(RotateTransform.AngleProperty, null);
        }




        //******************* UPLOAD ********************************

        // Upload 1 file và trả về objectid của bài hát đó
        // return id bài hát sau khi up lên cloud
        private ObjectId UploadFile(string filepath)
        {
            IGridFSBucket bucket = new GridFSBucket(CloudDB.database);
            Stream src = File.OpenRead(filepath);
            var id = bucket.UploadFromStream(System.IO.Path.GetFileName(filepath), src);
            //---MessageBox.Show("Upload Done!");
            src.Close();
            return id;
        }

        // playlist là 1 file txt chứ  path của bài hát trong máy
        // Return về id của playlist sau khi up lên cloud
        private ObjectId UploadPlaylist(string playlistpath)
        {
            var playlistInfo = new FileInfo(playlistpath);
            var reader = new StreamReader(playlistpath);
            // đường dẫn của bài hát trong máy
            string filepath = null;

            // tạo một đối tượng playlist
            Playlist playlist = new Playlist(playlistInfo.Name);

            // upload bài hát lên cloud và thêm id bài hát đó vào đối tượng playlist vừa mới tạo
            filepath = reader.ReadLine();
            while ((filepath = reader.ReadLine()) != null)
            {
                // Bước 1. up bài hát lên cloud
                var id = UploadFile(filepath);
                playlist.songs.Add(id.ToString());
            }
            reader.Close();
            // Bước 2. Đưa đối tượng playlist lên cloud
            var collectionPlaylist = CloudDB.database.GetCollection<Playlist>("playlist");
            collectionPlaylist.InsertOne(playlist);
            return playlist.Id;
        }

        // Cho phép chọn playlist trong máy để up lên cloud
        private void UploadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CloudDB.currentUser == null)
            {
                MessageBox.Show("Chưa đăng nhập");
            }
            else
            {
                var screen = new OpenFileDialog();
                screen.InitialDirectory = Environment.CurrentDirectory;
                screen.Filter = "Text Files (.txt)|*.txt";
                if (screen.ShowDialog() == true)
                {
                    var playlistpath = screen.FileName;
                    var size = new FileInfo(playlistpath).Length;
                    // Nếu playlist khác rỗng thì mới up
                    if (size != 0)
                    {
                        // Việc Upload này gồm 3 việc: 
                        // 1. Đưa bài hát trong playlist của máy local lên cloud
                        // 2. Đưa playlist lên cloud - (playlist này là 1 mảng các id bài hát đã tải lên cloud)
                        // 3. Gán id của playlist này cho User hiện tại rồi cập nhật lên cloud 
                        ObjectId id = UploadPlaylist(playlistpath);

                        // Bước 3.
                        var collectionUser = CloudDB.database.GetCollection<User>("user");
                        var filter = Builders<User>.Filter.Eq("username", CloudDB.currentUser.username);
                        var res = collectionUser.Find(filter).ToList<User>();

                        res[0].playlists.Add(id.ToString());
                        List<string> updatedPlaylist = res[0].playlists;
                        var update = Builders<User>.Update.Set("playlists", updatedPlaylist);
                        collectionUser.UpdateOne(filter, update);

                        MessageBox.Show("Upload thành công");
                    }
                }
            }
        }
        // **************************************************************************

        //************************** DOWNLOAD **************************************

        private void DownloadFile(ObjectId id, string dir)
        {
            IGridFSBucket bucket = new GridFSBucket(CloudDB.database);
            Stream destination = File.OpenWrite(dir);
            bucket.DownloadToStream(id, destination);
            destination.Close();
        }
        private void DownloadPlaylist(ObjectId objectId, string dir)
        {
            Playlist playlist = new Playlist();
            var collectionPlaylist = CloudDB.database.GetCollection<Playlist>("playlist");
            var filter = Builders<Playlist>.Filter.Eq("_id", objectId);
            var res = collectionPlaylist.Find(filter).ToList<Playlist>();
            playlist = res[0];
            var writer = new StreamWriter(dir + @"\" + playlist.playlistname);
            IList<string> songs = new List<string>();
            foreach (var id in playlist.songs)
            {
         
                var collectionSong = CloudDB.database.GetCollection<Song>("fs.files");
                var filter2 = Builders<Song>.Filter.Eq("_id", new ObjectId(id));
                var res2 = collectionSong.Find(filter2).ToList<Song>();
                var song = res2[0];

                DownloadFile(new ObjectId(id), dir + "\\" + song.filename);
                songs.Add(dir + @"\" + song.filename);                
            }
            writer.WriteLine(songs.Count);
            foreach(var path in songs)
            {
                writer.WriteLine(path);
            }
            writer.Close();
            
        }
        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CloudDB.currentUser == null)
            {
                MessageBox.Show("Chưa đăng nhập");
            }
            else
            {
                var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string dir = folderBrowserDialog.SelectedPath;
                    var collectionUser = CloudDB.database.GetCollection<User>("user");
                    var filter = Builders<User>.Filter.Eq("username", CloudDB.currentUser.username);
                    var result = collectionUser.Find(filter).ToList();
                    CloudDB.currentUser = result[0];
                    foreach (var id in CloudDB.currentUser.playlists)
                    {
                        DownloadPlaylist(new ObjectId(id), dir);
                    }
                    MessageBox.Show("Download thành công");
                }

            }
        }
        public static Label state;
        private void signinBtn_Click(object sender, RoutedEventArgs e)
        {
            state = profile;
            CloudDB cloudDBWindow = new CloudDB();
            cloudDBWindow.Show();         
        }

        private void SignoutBtn_Click(object sender, RoutedEventArgs e)
        {

            CloudDB.currentUser = null;
            profile.Content = "";

        }
        //-------------------------------------------------------------------

    }

    public class PlayList : INotifyPropertyChanged
    {
        private FileInfo mInfo;
        private bool mIsRemoved;
        private Brush mTextColor;

        public string FullName
        {
            get { return mInfo.FullName; }
        }

        public string Name
        {
            get { return mInfo.Name; }
        }

        public bool IsRemoved
        {
            get { return mIsRemoved; }
            set { mIsRemoved = value; }
        }

        public Brush TextColor
        {
            get { return mTextColor; }
            set
            {
                if (this.mTextColor != value)
                {
                    mTextColor = value;
                    this.NotifyPropertyChanged("TextColor");
                }
            }
        }

        public PlayList(FileInfo info, bool isRemoved, Brush textColor)
        {
            this.mInfo = info;
            this.mIsRemoved = isRemoved;
            this.mTextColor = textColor;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class TimerSliderLabel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
        public TimerSliderLabel()
        {
            labelString = "";
            sliderDouble = 0;
        }
        private string labelString;
        public string LabelString
        {
            get { return labelString; }
            set
            {
                labelString = value;
                OnPropertyChanged("LabelString");
            }
        }
        private double sliderDouble;
        public double SliderDouble
        {
            get { return sliderDouble; }
            set
            {
                //if (sliderDouble != value)
                //{
                sliderDouble = value;
                OnPropertyChanged("SliderDouble");
                var curPos = TimeSpan.FromMilliseconds(sliderDouble / 100 * MainWindow.mMediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds);
                var endPos = MainWindow.mMediaPlayer.NaturalDuration.TimeSpan;
                labelString = curPos.ToString(@"mm\:ss") + " / " + endPos.ToString(@"mm\:ss");
                OnPropertyChanged("LabelString");
                //}
            }
        }

    }

}
