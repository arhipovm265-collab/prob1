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
    public partial class OrdersWindow : Window
    {
        private bool isAdmin;

        public OrdersWindow(User user)
        {
            InitializeComponent();
            isAdmin = user.Role.Name == "Администратор";
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                AppDbContext db = new AppDbContext();
                var orders = db.Orders
                    .Include(order => order.User)
                    .OrderByDescending(order => order.OrderDate)
                    .ThenByDescending(order => order.Id)
                    .ToList();
                List<OrderView> list = new List<OrderView>();

                foreach (var order in orders)
                {
                    OrderView orderView = new OrderView();
                    orderView.Id = order.Id;
                    orderView.DateText = order.OrderDate.ToString("dd.MM.yyyy");
                    orderView.ClientName = order.User.LastName + " " + order.User.FirstName + " " + order.User.MiddleName;
                    list.Add(orderView);
                }
                gridOrders.ItemsSource = list;
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить список заказов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonView_Click(object sender, RoutedEventArgs e)
        {
            OrderView selected = gridOrders.SelectedItem as OrderView;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ в таблице", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OrderDetailsWindow window = new OrderDetailsWindow(selected.Id, isAdmin);
            window.Owner = this;
            window.ShowDialog();
            LoadOrders();
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            OrderView selected = gridOrders.SelectedItem as OrderView;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult answer = MessageBox.Show("Удалить заказ №" + selected.Id + "? Товары вернутся на склад", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.No)
            {
                return;
            }
            try
            {
                AppDbContext db = new AppDbContext();
                Order order = db.Orders
                    .Include(o => o.OrderItems).ThenInclude(i => i.StockItem)
                    .First(o => o.Id == selected.Id);
                foreach (OrderItem orderItem in order.OrderItems)
                {
                    orderItem.StockItem.Quantity += orderItem.Quantity;
                }
                db.OrderItems.RemoveRange(order.OrderItems);
                db.Orders.Remove(order);
                db.SaveChanges();
                LoadOrders();
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось удалить заказ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class OrderView
    {
        public int Id { get; set; }
        public string DateText { get; set; }
        public string ClientName { get; set; }
    }
}