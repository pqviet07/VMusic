using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace _1712907_1712912_1712914
{
    /// <summary>
    /// Interaction logic for Cloud.xaml
    /// </summary>
    public partial class CloudDB : Window
    {
        public static User currentUser { set; get; }
        public static IMongoDatabase database;
        public CloudDB()
        {
            InitializeComponent();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            MongoClient dbClient = new MongoClient("mongodb+srv://vietpro07:Vietpro07.@cluster0-nium1.gcp.mongodb.net/test");
            //MongoClient dbClient = new MongoClient("mongodb://127.0.0.1:27017");           
            database = dbClient.GetDatabase("VMusic");
            currentUser = null;
        }
        private bool isValidUser()
        {
            string username = UsernameText_Wpf.Text;
            string password = PasswordText_Wpf.Password;
            if (username.Length == 0 || password.Length == 0)
            {
                return false;
            }
            var collectionUser = database.GetCollection<User>("user");
            var filter = Builders<User>.Filter.Eq("username", username);
            var result = collectionUser.Find(filter).ToList();
            if (result.Count == 0)
            {
                return false;
            }

            currentUser = result[0];
            // var passwordHashed = BCrypt.Net.BCrypt.HashPassword(username + password);
            if (BCrypt.Net.BCrypt.Verify(password + username, currentUser.password))
            {
                return true;
            }
            return false;
        }

        // Đảm bảo trên 8 ký tự bao gồm chữ hoa, chữ thường, số, ký tự đặc biệt
        private bool isStrongPassword(string password)
        {
            string pattern = @"(?=^.{8,}$)((?=.*\d)|(?=.*\W+))(?![.\n])(?=.*[A-Z])(?=.*[a-z]).*$";
            Regex rg = new Regex(pattern);
            return rg.IsMatch(password);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseToggleButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow.Close();
        }

        private void SigninRadioBtn_Click(object sender, RoutedEventArgs e)
        {
            signinBtn.Visibility = Visibility.Visible;
            signupBtn.Visibility = Visibility.Hidden;
            rememberCheckbox.Visibility = Visibility.Visible;
            ico.Visibility = Visibility.Hidden;
            PasswordConfirmText_Wpf.Visibility = Visibility.Hidden;

            UsernameText_Wpf.Text = "";
            PasswordText_Wpf.Password = "";
            title.Text = "Login";
        }

        private void SignupRadioBtn_Click(object sender, RoutedEventArgs e)
        {
            signinBtn.Visibility = Visibility.Hidden;
            signupBtn.Visibility = Visibility.Visible;
            rememberCheckbox.Visibility = Visibility.Hidden;
            ico.Visibility = Visibility.Visible;
            PasswordConfirmText_Wpf.Visibility = Visibility.Visible;

            UsernameText_Wpf.Text = "";
            PasswordText_Wpf.Password = "";
            PasswordConfirmText_Wpf.Password = "";
            title.Text = "Sign up";
        }

        private void SigninBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isValidUser())
            {
                MessageBox.Show("Đăng nhập thành công");
                this.Close();
            }
            else
                MessageBox.Show("Đăng nhập thất bại");
        }

        private void SignupBtn_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameText_Wpf.Text;
            if (username.Length != 0)
            {
                var collectionUser = database.GetCollection<User>("user");
                var filter = Builders<User>.Filter.Eq("username", username);
                var result = collectionUser.Find(filter).ToList();
                // result.Count == 0 nghĩa là chưa có tài này
                if (result.Count == 0)
                {
                    if (isStrongPassword(PasswordText_Wpf.Password))
                    {
                        if (PasswordText_Wpf.Password == PasswordConfirmText_Wpf.Password)
                        {
                            //
                            var user = new User(username, BCrypt.Net.BCrypt.HashPassword(PasswordText_Wpf.Password + username));
                            collectionUser.InsertOne(user);
                            validIco.Visibility = Visibility.Visible;
                            MessageBox.Show("Đăng ký thành công");
                            this.Close();
                        }
                        else
                        {
                            message.Content = "Password không khớp";
                            invalidIco.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        message.Content = "Password ít nhất 8 kí tự, gồm hoa, thường, số, kí tự đặc biệt";
                    }
                }
                else
                {
                    message.Content = "Tài khoản này đã tồn tại";
                }
            }
            else
            {
                message.Content = "Tài khoản không được để trống";
            }
        }

        private void UsernameText_Wpf_GotFocus(object sender, RoutedEventArgs e)
        {
            message.Content = "";
        }

        private void PasswordText_Wpf_GotFocus(object sender, RoutedEventArgs e)
        {
            message.Content = "";
        }

        private void PasswordConfirmText_Wpf_GotFocus(object sender, RoutedEventArgs e)
        {
            message.Content = "";
            invalidIco.Visibility = Visibility.Hidden;
            validIco.Visibility = Visibility.Hidden;
        }

        private void LoginWindow_Closed(object sender, EventArgs e)
        {
            if (currentUser != null)
            {
                MainWindow.state.Content = $"Xin chào: { currentUser.username}";
            }
        }
    }

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { set; get; }

        public string username { get; set; }

        public string password { get; set; }

        // chứa id của playlist, mỗi phần tử của playlists là 1 playlist
        public List<string> playlists { get; set; }
        public User()
        {
            Id = ObjectId.GenerateNewId();
            playlists = new List<string>();
        }
        public User(string usrn)
        {
            Id = ObjectId.GenerateNewId();
            username = usrn;
            playlists = new List<string>();
        }
        public User(string usrn, string pwd)
        {
            Id = ObjectId.GenerateNewId();
            username = usrn;
            password = pwd;
            playlists = new List<string>();
        }

        public override string ToString()
        {
            return "User: " + Id + "; " + username;
        }
    }

    public class Playlist
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { set; get; }
        public string playlistname { get; set; }

        // chứa Id của bài hát, mỗi phần tử của List songs là 1 bài hát
        public List<string> songs { get; set; }

        public Playlist()
        {
            Id = ObjectId.GenerateNewId();
            songs = new List<string>();
        }
        public Playlist(string name)
        {
            Id = ObjectId.GenerateNewId();
            playlistname = name;
            songs = new List<string>();
        }
    }

    public class Song
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { set; get; }
        public long length { get; set; }
        public int chunkSize { get; set; }
        public DateTime uploadDate { get; set; }
        public string md5 { get; set; }
        public string filename { get; set; }
    }
}
