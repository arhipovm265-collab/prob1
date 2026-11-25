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
    public partial class ProductWindow : Window
    {
        private Product product;
        private decimal price;
        private List<CartLine> cart;
        private List<StockItem> availableStock = new List<StockItem>();
        public ProductWindow(int productId, List<CartLine> cart)
        {
            InitializeComponent();
            this.cart = cart;
            LoadProduct(productId);
        }
        private void LoadProduct(int productId)
        {
            AppDbContext db = new AppDbContext();
            product = db.Products
                .Include(p => p.Manufacturer)
                .Include(p => p.Subcategory).ThenInclude(s => s.Category)
                .Include(p => p.StockItems).ThenInclude(s => s.Size)
                .First(p => p.Id == productId);
            string lastMonth = DateTime.Today.AddMonths(-1).ToString("yyyy-MM");
            bool wasOrdered = false;
            var orderItems = db.OrderItems.Include(item => item.Order).Include(item => item.StockItem).ToList();
            foreach (var orderItem in orderItems)
            {
                if (orderItem.StockItem.ProductId == product.Id && orderItem.Order.OrderDate.ToString("yyyy-MM") == lastMonth)
                {
                    wasOrdered = true;
                }
            }

            price = product.Price;
            if (!wasOrdered)
            {
                price = Math.Round(price * 0.75m, 2);
            }
            imagePhoto.Source = new BitmapImage(new Uri("pack://application:,,,/Images/" + product.ImageFileName));
            textManufacturer.Text = "Производство: " + product.Manufacturer.Name;
            textName.Text = product.Name;
            textCategory.Text = "Категория: " + product.Subcategory.Category.Name;
            textPrice.Text = price.ToString("0.00") + " руб.";
            textComposition.Text = "Состав: " + product.Composition;
            textDescription.Text = "Описание: " + product.Description;
            var allSizes = product.StockItems.Select(s => s.Size.Value).ToList();
            textSizeRange.Text = "Размерный ряд: " + allSizes.Min().ToString("0.#") + " - " + allSizes.Max().ToString("0.#");
            availableStock = product.StockItems.Where(s => s.Quantity > 0).OrderBy(s => s.Size.Value).ToList();
            comboSizes.ItemsSource = availableStock.Select(s => s.Size.Value.ToString("0.#")).ToList();
            if (availableStock.Count == 0)
            {
                textAvailable.Text = "Нет в наличии";
            }
            else
            {
                comboSizes.SelectedIndex = 0;
            }
        }
        private void ComboSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboSizes.SelectedIndex < 0)
            {
                return;
            }
            textAvailable.Text = "В наличии: " + availableStock[comboSizes.SelectedIndex].Quantity + " шт.";
        }
        private void TextQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (comboSizes.SelectedIndex < 0)
            {
                MessageBox.Show("Выберите размер обуви из списка", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int quantity;
            if (!int.TryParse(textQuantity.Text, out quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите количество пар", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            StockItem stockItem = availableStock[comboSizes.SelectedIndex];
            CartLine line = cart.FirstOrDefault(l => l.StockItemId == stockItem.Id);
            int inCart = 0;
            if (line != null)
            {
                inCart = line.Quantity;
            }

            if (inCart + quantity > stockItem.Quantity)
            {
                MessageBox.Show("Доступно " + stockItem.Quantity + " уменьшите количество", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (line == null)
            {
                line = new CartLine();
                line.StockItemId = stockItem.Id;
                line.ProductName = product.Manufacturer.Name + " | " + product.Name;
                line.SizeText = stockItem.Size.Value.ToString("0.#");
                line.UnitPrice = price;
                line.Available = stockItem.Quantity;
                line.Quantity = quantity;
                cart.Add(line);
            }
            else
            {
                line.Quantity += quantity;
            }
            MessageBox.Show("Товар добавлен в заказ.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}