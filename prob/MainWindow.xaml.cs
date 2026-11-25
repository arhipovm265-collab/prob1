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
    public partial class MainWindow : Window
    {
        private User currentUser;
        private List<CartLine> cart = new List<CartLine>();
        private List<ProductView> allProducts;
        private string orderButtonName = "Мой заказ";
        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            if (currentUser == null)
            {
                textUserName.Text = "Неавторизованный пользователь";
                buttonOrder.Visibility = Visibility.Collapsed;
                buttonManageOrders.Visibility = Visibility.Collapsed;
            }
            else
            {
                textUserName.Text = currentUser.LastName + " " + currentUser.FirstName + " " + currentUser.MiddleName;
                string roleName = currentUser.Role.Name;
                if (roleName != "Администратор" && roleName != "Менеджер")
                {
                    buttonManageOrders.Visibility = Visibility.Collapsed;
                }
                else
                {
                    orderButtonName = "Новый заказ";
                }
            }
            try
            {
                LoadCategories();
                LoadProducts();
            }
            catch (Exception)
            {
                MessageBox.Show("Неудалось загрузить бд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            AppDbContext db = new AppDbContext();
            comboCategory.Items.Add("Все категории");

            foreach (var category in db.Categories.OrderBy(c => c.Name).ToList())
            {
                comboCategory.Items.Add(category.Name);
            }
            comboCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            AppDbContext db = new AppDbContext();
            string lastMonth = DateTime.Today.AddMonths(-1).ToString("yyyy-MM");
            List<int> orderedProductIds = new List<int>();
            var orderItems = db.OrderItems.Include(item => item.Order).Include(item => item.StockItem).ToList();
            foreach (var orderItem in orderItems)
            {
                if (orderItem.Order.OrderDate.ToString("yyyy-MM") == lastMonth)
                {
                    orderedProductIds.Add(orderItem.StockItem.ProductId);
                }
            }
            var products = db.Products
                .Include(product => product.Manufacturer)
                .Include(product => product.Subcategory).ThenInclude(subcategory => subcategory.Category)
                .Include(product => product.StockItems)
                .ToList();
            allProducts = new List<ProductView>();

            foreach (var product in products)
            {
                int totalPairs = product.StockItems.Sum(stockItem => stockItem.Quantity);
                decimal price = product.Price;
                if (!orderedProductIds.Contains(product.Id))
                {
                    price = Math.Round(price * 0.75m, 2);
                }

                ProductView productView = new ProductView();
                productView.Id = product.Id;
                productView.Name = product.Name;
                productView.Description = product.Description ?? "";
                productView.Title = product.Manufacturer.Name + " | " + product.Name;
                productView.Category = product.Subcategory.Category.Name;
                productView.Composition = product.Composition;
                productView.PriceValue = price;
                productView.Price = price.ToString("0.00") + " руб.";
                productView.Quantity = $"Мало ({totalPairs})";
                productView.Background = Brushes.White;

                if (totalPairs > 5)
                {
                    productView.Quantity = $"Много ({totalPairs})";
                }
                if (totalPairs <= 3)
                {
                    productView.Background = new SolidColorBrush(Color.FromRgb(255, 128, 128));
                }
                productView.Photo = new BitmapImage(new Uri("pack://application:,,,/Images/" + product.ImageFileName));
                allProducts.Add(productView);
            }
            ShowProducts();
            UpdateOrderButton();
        }
        private void ShowProducts()
        {
            if (allProducts == null)
            {
                return;
            }

            IEnumerable<ProductView> result = allProducts;

            if (comboCategory.SelectedIndex > 0)
            {
                string category = comboCategory.SelectedItem.ToString();
                result = result.Where(p => p.Category == category);
            }
            string search = textSearch.Text.Trim().ToLower();
            if (search != "")
            {
                result = result.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
            }
            if (comboSort.SelectedIndex == 1)
            {
                result = result.OrderBy(p => p.PriceValue);
            }
            if (comboSort.SelectedIndex == 2)
            {
                result = result.OrderByDescending(p => p.PriceValue);
            }
            List<ProductView> list = result.ToList();
            listProducts.ItemsSource = list;

            if (list.Count == 0)
            {
                textEmpty.Visibility = Visibility.Visible;
            }
            else
            {
                textEmpty.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateOrderButton()
        {
            buttonOrder.Content = orderButtonName + " (" + cart.Count + ")";
        }
        private void TextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowProducts();
        }
        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowProducts();
        }
        private void ComboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowProducts();
        }
        private void ListProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProductView selected = listProducts.SelectedItem as ProductView;
            if (selected == null)
            {
                return;
            }
            listProducts.SelectedItem = null;

            if (currentUser == null)
            {
                MessageBox.Show("Войдите под логином","Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ProductWindow window = new ProductWindow(selected.Id, cart);
            window.Owner = this;
            window.ShowDialog();
            UpdateOrderButton();
        }
        private void ButtonOrder_Click(object sender, RoutedEventArgs e)
        {
            CartWindow window = new CartWindow(cart, currentUser);
            window.Owner = this;
            window.ShowDialog();

            try
            {
                LoadProducts();
            }
            catch (Exception)
            {
                MessageBox.Show("Неудалось обновить каталог","Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonManageOrders_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow window = new OrdersWindow(currentUser);
            window.Owner = this;
            window.ShowDialog();

            try
            {
                LoadProducts();
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось обновить каталог", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ButtonExit_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count > 0)
            {
                MessageBoxResult answer = MessageBox.Show("Заказ будет очищен", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer == MessageBoxResult.No)
                {
                    return;
                }
            }
            LoginWindow window = new LoginWindow();
            window.Show();
            Close();
        }
    }
    public class ProductView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BitmapImage Photo { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Quantity { get; set; }
        public string Composition { get; set; }
        public string Price { get; set; }
        public decimal PriceValue { get; set; }
        public Brush Background { get; set; }
    }
    public class CartLine
    {
        public int StockItemId { get; set; }
        public string ProductName { get; set; }
        public string SizeText { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int Available { get; set; }
        public string PriceText
        {
            get { return UnitPrice.ToString("0.00") + " руб."; }
        }
        public string TotalText
        {
            get { return (UnitPrice * Quantity).ToString("0.00") + " руб."; }
        }
    }
}