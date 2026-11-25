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

namespace prob
{
    public partial class CartWindow : Window
    {
        private List<CartLine> cart;
        private User currentUser;
        private List<User> clients;
        private bool isStaff;

        public CartWindow(List<CartLine> cart, User user)
        {
            InitializeComponent();
            this.cart = cart;
            currentUser = user;
            isStaff = user.Role.Name == "Администратор" || user.Role.Name == "Менеджер";
            if (isStaff)
            {
                LoadClients();
            }
            else
            {
                panelClient.Visibility = Visibility.Collapsed;
            }
            ShowCart();
        }
        private void LoadClients()
        {
            try
            {
                AppDbContext db = new AppDbContext();
                clients = db.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();
                comboClients.ItemsSource = clients.Select(u => u.LastName + " " + u.FirstName + " " + u.MiddleName).ToList();
                datePickerOrder.SelectedDate = DateTime.Today;
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить список клиентов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ShowCart()
        {
            gridCart.ItemsSource = null;
            gridCart.ItemsSource = cart.ToList();
            decimal total = cart.Sum(line => line.UnitPrice * line.Quantity);
            textTotal.Text = "Итого: " + total.ToString("0.00") + " руб.";
        }

        private void TextNewQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char symbol in e.Text)
            {
                if (!char.IsDigit(symbol))
                {
                    e.Handled = true;
                    textHint.Text = "В поле количества можно вводить только цифры";
                    return;
                }
            }
            textHint.Text = "";
        }
        private void ButtonChangeQuantity_Click(object sender, RoutedEventArgs e)
        {
            CartLine line = gridCart.SelectedItem as CartLine;
            if (line == null)
            {
                MessageBox.Show("Выберите позицию в таблице, количество которой нужно изменить", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int quantity;
            if (!int.TryParse(textNewQuantity.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите количество пар: целое число, больше нуля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (quantity > line.Available)
            {
                MessageBox.Show("Для этого размера столько нет. Уберите до " + line.Available, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            line.Quantity = quantity;
            textNewQuantity.Text = "";
            ShowCart();
        }
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            CartLine line = gridCart.SelectedItem as CartLine;
            if (line == null)
            {
                MessageBox.Show("Выберите позицию для удаления","Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            cart.Remove(line);
            ShowCart();
        }

        private void ButtonCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                return;
            }

            MessageBoxResult answer = MessageBox.Show("Отказаться от заказа?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Yes)
            {
                cart.Clear();
                ShowCart();
            }
        }

        private void ButtonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Заказ пуст, добавьте товары", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int clientId = currentUser.Id;
            DateOnly orderDate = DateOnly.FromDateTime(DateTime.Today);
            if (isStaff)
            {
                if (comboClients.SelectedIndex < 0)
                {
                    MessageBox.Show("Выберите клиента", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (datePickerOrder.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату заказа", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DateTime date = datePickerOrder.SelectedDate.Value;
                if (date > DateTime.Today)
                {
                    MessageBox.Show("Дата заказа не может быть позже сегодняшней", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                clientId = clients[comboClients.SelectedIndex].Id;
                orderDate = DateOnly.FromDateTime(date);
            }
            MessageBoxResult answer = MessageBox.Show("Подтвердить оформление заказа?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                AppDbContext db = new AppDbContext();
                Order order = new Order();
                order.OrderDate = orderDate;
                order.UserId = clientId;

                foreach (CartLine line in cart)
                {
                    StockItem stockItem = db.StockItems.Find(line.StockItemId);
                    if (stockItem.Quantity < line.Quantity)
                    {
                        MessageBox.Show("Для позиции «" + line.ProductName + "» размера " + line.SizeText + " осталось только " + stockItem.Quantity + " пар. Измените количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        line.Available = stockItem.Quantity;
                        return;
                    }
                    stockItem.Quantity -= line.Quantity;
                    OrderItem orderItem = new OrderItem();
                    orderItem.StockItemId = line.StockItemId;
                    orderItem.Quantity = line.Quantity;
                    orderItem.UnitPrice = line.UnitPrice;
                    order.OrderItems.Add(orderItem);
                }

                db.Orders.Add(order);
                db.SaveChanges();
                cart.Clear();
                MessageBox.Show("Заказ оформлен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Неудалось оформить заказ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}