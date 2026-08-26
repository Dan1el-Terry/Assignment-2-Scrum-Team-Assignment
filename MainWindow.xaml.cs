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

            try {
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
                    throw new Exception ("Invalid username or password.");
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void NewUserScreenButton_Click(object sender, RoutedEventArgs e)
        {
            AdminScreen.Visibility = Visibility.Collapsed;
            NewUserScreen.Visibility = Visibility.Visible;
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UserDatabase.Register(NewUsername.Text, NewPassword.Password, NewRole.Text))
                {
                    MessageBox.Show("Registration successful! You can now log in.");
                    NewUsername.Clear();
                    NewPassword.Clear();
                    NewRole.SelectedItem = null;
                }
                else
                {
                    throw new Exception("Registration failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AdminScreen.Visibility = Visibility.Collapsed;
            UserScreen.Visibility = Visibility.Collapsed;
            LoginScreen.Visibility = Visibility.Visible;
        }
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            NewUserScreen.Visibility = Visibility.Collapsed;
            AdminScreen.Visibility = Visibility.Visible;
        }
    }
}