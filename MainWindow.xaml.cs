using System.Windows;

namespace Assign_2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserDatabase.Login(UsernameBox.Text, PasswordBox.Password, out string role))
            {
                // Clear inputs
                UsernameBox.Clear();
                PasswordBox.Clear();
                LoginScreen.Visibility = Visibility.Collapsed;

                // Route to correct view
                if (role == "Admin")
                {
                    AdminScreen.Visibility = Visibility.Visible;
                }
                else if (role == "User")
                {
                    UserScreen.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Error");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AdminScreen.Visibility = Visibility.Collapsed;
            UserScreen.Visibility = Visibility.Collapsed;
            LoginScreen.Visibility = Visibility.Visible;
        }
    }
}