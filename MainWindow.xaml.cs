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
using System.Windows.Shapes;

namespace Assign_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Define a 2D array to store account details (username, password, role) THIS IS TEMPORARY AND WILL BE REPLACED WITH A DATABASE IN THE FUTURE
            string[][] accountDetails = new string[][]
            {
                new string[] { "admin", "password1", "Admin"},
                new string[] { "guest", "password2", "Guest"}
            };
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;
            try
            {
                for (int i = 0; i < accountDetails.Length; i++)
                {
                    //
                    if (username == accountDetails[i][0] && password == accountDetails[i][1])
                    {
                        MessageBox.Show("Login successful!");
                        LoginScreen.Visibility = Visibility.Collapsed;
                        //Checks Role
                        if (accountDetails[i][2] == "Admin")
                        {
                            AdminScreen.Visibility = Visibility.Visible;
                        }
                        else if (accountDetails[i][2] == "Guest")
                        {
                            GuessScreen.Visibility = Visibility.Visible;
                        }
                        return;
                    }

                }
                throw new Exception("Invalid username or password.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide the admin and guest screens
            AdminScreen.Visibility = Visibility.Collapsed;
            GuessScreen.Visibility = Visibility.Collapsed;
            // Show the login screen
            LoginScreen.Visibility = Visibility.Visible;
            // Clear the username and password fields
            UsernameBox.Text = string.Empty;
            PasswordBox.Password = string.Empty;
        }
    }
}
