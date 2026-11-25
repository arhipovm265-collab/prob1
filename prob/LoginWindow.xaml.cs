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
using Microsoft.EntityFrameworkCore;

namespace prob
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = textLogin.Text.Trim();

            if (login == "")
            {
                MessageBox.Show("Введите логин", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                AppDbContext db = new AppDbContext();
                User user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Login == login);

                if (user == null)
                {
                    MessageBox.Show("«" + login + "» не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OpenCatalog(user);
            }
            catch (Exception)
            {
                MessageBox.Show("Неудалось загрузить бд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonGuest_Click(object sender, RoutedEventArgs e)
        {
            OpenCatalog(null);
        }

        private void OpenCatalog(User user)
        {
            MainWindow window = new MainWindow(user);
            window.Show();
            Close();
        }
    }
}