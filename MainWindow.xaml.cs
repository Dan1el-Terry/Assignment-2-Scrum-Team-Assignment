using System;
using System.Windows;
using System.Windows.Controls;

namespace Assign_2
{
    public partial class MainWindow : Window
    {
        private UserRecord selectedUser;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string role;

            bool success = UserDatabase.Login(username, password, out role);

            if (success == false)
            {
                MessageBox.Show("Invalid username or password.");
                return;
            }

            if (role == "Admin")
            {
                OpenAdminScreen();
            }
            else
            {
                ShowScreen(UserScreen);
            }
        }

        private void OpenAdminScreen()
        {
            ShowScreen(AdminScreen);
            RefreshUserList();
        }

        private void RefreshUserList()
        {
            UsersGrid.ItemsSource = UserDatabase.GetAllUsers();
        }
        //clickable table
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersGrid.SelectedItem as UserRecord;

            if (selectedUser == null)
            {
                return;
            }

            EditUsernameBox.Text = selectedUser.Username;
            NewPasswordBox.Password = "";

            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if (item.Content.ToString() == selectedUser.Role)
                {
                    RoleBox.SelectedItem = item;
                }
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            string newUsername = EditUsernameBox.Text.Trim();
            ComboBoxItem chosenRole = RoleBox.SelectedItem as ComboBoxItem;

            if (newUsername == "" || chosenRole == null)
            {
                MessageBox.Show("Username and role are required.");
                return;
            }

            string newRole = chosenRole.Content.ToString();

            try
            {
                UserDatabase.UpdateUser(selectedUser.Id, newUsername, newRole);
                RefreshUserList();
                MessageBox.Show("User updated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            if (NewPasswordBox.Password == "")
            {
                MessageBox.Show("Enter a new password.");
                return;
            }

            UserDatabase.UpdatePassword(selectedUser.Id, NewPasswordBox.Password);
            NewPasswordBox.Password = "";
            MessageBox.Show("Password updated.");
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Delete user '" + selectedUser.Username + "'?",
                "Confirm Delete",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                UserDatabase.DeleteUser(selectedUser.Id);
                RefreshUserList();
            }
        }

        private void ShowNewUserScreen_Click(object sender, RoutedEventArgs e)
        {
            ShowScreen(NewUserScreen);
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsername.Text.Trim();
            string password = NewPassword.Password;
            ComboBoxItem chosenRole = NewRole.SelectedItem as ComboBoxItem;

            if (username == "" || password == "" || chosenRole == null)
            {
                MessageBox.Show("All fields are required.");
                return;
            }

            string role = chosenRole.Content.ToString();

            try
            {
                UserDatabase.Register(username, password, role);
                MessageBox.Show("User registered.");
                NewUsername.Clear();
                NewPassword.Clear();
                NewRole.SelectedItem = null;
                OpenAdminScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAdminScreen();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ShowScreen(LoginScreen);
        }

        private void ShowScreen(UIElement screenToShow)
        {
            LoginScreen.Visibility = Visibility.Collapsed;
            AdminScreen.Visibility = Visibility.Collapsed;
            NewUserScreen.Visibility = Visibility.Collapsed;
            UserScreen.Visibility = Visibility.Collapsed;

            screenToShow.Visibility = Visibility.Visible;
        }
    }
}