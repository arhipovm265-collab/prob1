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
    public partial class OrderDetailsWindow : Window
    {
        private int orderId;

        public OrderDetailsWindow(int orderId, bool canEdit)
        {
            InitializeComponent();
            this.orderId = orderId;
            if (!canEdit)
            {
                panelEdit.Visibility = Visibility.Collapsed;
            }
            LoadOrder();
            LoadAvailableStock();
        }

        private void LoadOrder()
        {
            try
            {
                using AppDbContext db = new AppDbContext();
                Order order = db.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(i => i.StockItem)
                    .ThenInclude(s => s.Product)
                    .ThenInclude(p => p.Manufacturer)
                    .Include(o => o.OrderItems)
                    .ThenInclude(i => i.StockItem)
                    .ThenInclude(s => s.Size)
                    .First(o => o.Id == orderId);

                textOrderInfo.Text = "Заказ №" + order.Id +  " от " + order.OrderDate.ToString("dd.MM.yyyy") + ", клиент: " + order.User.LastName + " " + order.User.FirstName + " " + order.User.MiddleName;
                datePickerOrder.SelectedDate = order.OrderDate.ToDateTime(TimeOnly.MinValue);
                List<OrderLineView> lines = new List<OrderLineView>();
                decimal total = 0;
                foreach (OrderItem orderItem in order.OrderItems)
                {
                    OrderLineView line = new OrderLineView();
                    line.OrderItemId = orderItem.Id;
                    line.ProductName = orderItem.StockItem.Product.Manufacturer.Name + " | " + orderItem.StockItem.Product.Name;
                    line.SizeText = orderItem.StockItem.Size.Value.ToString("0.#");
                    line.Quantity = orderItem.Quantity;
                    line.PriceText = orderItem.UnitPrice.ToString("0.00") + " руб.";
                    line.TotalText = (orderItem.UnitPrice * orderItem.Quantity).ToString("0.00") + " руб.";
                    lines.Add(line);
                    total += orderItem.UnitPrice * orderItem.Quantity;
                }
                gridLines.ItemsSource = lines;
                textTotal.Text = "Итого: " + total.ToString("0.00") + " руб.";
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить состав заказа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableStock()
        {
            try
            {
                using AppDbContext db = new AppDbContext();

                var stockItems = db.StockItems
                    .Include(s => s.Product)
                    .ThenInclude(p => p.Manufacturer)
                    .Include(s => s.Size)
                    .Where(s => s.Quantity > 0)
                    .ToList();

                List<StockChoice> choices = new List<StockChoice>();
                foreach (var item in stockItems)
                {
                    choices.Add(new StockChoice
                    {
                        Id = item.Id,
                        DisplayName = item.Product.Manufacturer.Name + " | " + item.Product.Name + " | Размер: " + item.Size.Value.ToString("0.#") + " | В наличии: " + item.Quantity
                    });
                }
                comboStock.ItemsSource = choices;
                if (choices.Count > 0)
                    comboStock.SelectedIndex = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить товары со склада", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonAddLine_Click(object sender, RoutedEventArgs e)
        {
            if (comboStock.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                StockChoice selected = comboStock.SelectedItem as StockChoice;
                using AppDbContext db = new AppDbContext();
                StockItem stockItem = db.StockItems .Include(s => s.Product) .FirstOrDefault(s => s.Id == selected.Id);
                if (stockItem == null)
                {
                    MessageBox.Show("Товар не найден на складе");
                    return;
                }
                if (stockItem.Quantity < quantity)
                {
                    MessageBox.Show("Недостаточно товара на складе. В наличии: " + stockItem.Quantity, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                OrderItem existingItem = db.OrderItems .FirstOrDefault(i => i.OrderId == orderId && i.StockItemId == stockItem.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    OrderItem newItem = new OrderItem
                    {
                        OrderId = orderId,
                        StockItemId = stockItem.Id,
                        Quantity = quantity,
                        UnitPrice = stockItem.Product.Price
                    };
                    db.OrderItems.Add(newItem);
                }
                stockItem.Quantity -= quantity;
                db.SaveChanges();
                MessageBox.Show("Товар успешно добавлен в заказ", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                txtQuantity.Text = "1";
                LoadOrder();
                LoadAvailableStock();
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось добавить товар в заказ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonSaveDate_Click(object sender, RoutedEventArgs e)
        {
            if (datePickerOrder.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату заказа");
                return;
            }
            DateTime date = datePickerOrder.SelectedDate.Value;
            if (date > DateTime.Today)
            {
                MessageBox.Show("Дата заказа не может быть позже сегодняшней");
                return;
            }

            try
            {
                using AppDbContext db = new AppDbContext();
                Order order = db.Orders.Find(orderId);
                order.OrderDate = DateOnly.FromDateTime(date);
                db.SaveChanges();
                MessageBox.Show("Дата заказа изменена");
                LoadOrder();
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось изменить дату");
            }
        }
        private void ButtonDeleteLine_Click(object sender, RoutedEventArgs e)
        {
            OrderLineView selected = gridLines.SelectedItem as OrderLineView;
            if (selected == null)
            {
                MessageBox.Show("Выберите позицию для удаления");
                return;
            }
            MessageBoxResult answer = MessageBox.Show( "Удалить позицию из заказа? Товар вернётся на склад", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.No)
                return;
            try
            {
                using AppDbContext db = new AppDbContext();
                OrderItem orderItem = db.OrderItems
                    .Include(i => i.StockItem)
                    .First(i => i.Id == selected.OrderItemId);
                orderItem.StockItem.Quantity += orderItem.Quantity;
                db.OrderItems.Remove(orderItem);
                db.SaveChanges();
                LoadOrder();
                LoadAvailableStock();
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось удалить позицию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class OrderLineView
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string SizeText { get; set; }
        public int Quantity { get; set; }
        public string PriceText { get; set; }
        public string TotalText { get; set; }
    }

    public class StockChoice
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
    }
}