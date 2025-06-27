using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BuildingMaterialsInventory
{
    public partial class Form1 : Form
    {
        // Шлях до файлу для збереження/завантаження товарів
        private static readonly string FilePath = Path.Combine(Application.StartupPath, "products.txt");

        // Колекція товарів в оперативній пам'яті
        private List<Product> products = new List<Product>();

        // Конструктор форми
        public Form1()
        {
            InitializeComponent();  // Ініціалізація компонентів форми (генерується дизайнером)
            this.Load += Form1_Load; // Підписка на подію завантаження форми, щоб завантажити товари з файлу при старті
        }

        // Внутрішній клас для представлення товару
        public class Product
        {
            public string Name { get; set; }          // Назва товару
            public string Type { get; set; }          // Тип товару (наприклад, Фарби)
            public string Manufacturer { get; set; }  // Виробник товару
            public int Quantity { get; set; }         // Кількість на складі
            public decimal Price { get; set; }        // Ціна товару

            // Перевизначення методу ToString для збереження товару у файл у вигляді рядка
            public override string ToString()
            {
                // Формат: Назва|Тип|Виробник|Кількість|Ціна
                return $"{Name}|{Type}|{Manufacturer}|{Quantity}|{Price}";
            }

            // Статичний метод для створення товару з рядка з файлу
            public static Product FromString(string line)
            {
                var parts = line.Split('|');  // Розбиваємо рядок за роздільником '|'
                return new Product
                {
                    Name = parts[0],                         // Назва з першої частини рядка
                    Type = parts[1],                         // Тип з другої частини
                    Manufacturer = parts[2],                 // Виробник з третьої частини
                    Quantity = int.Parse(parts[3]),          // Кількість конвертуємо у int
                    Price = decimal.Parse(parts[4])          // Ціну конвертуємо у decimal
                };
            }
        }

        // Обробник події завантаження форми
        private void Form1_Load(object sender, EventArgs e)
        {
            // Заповнюємо комбобокси типами товарів
            cmbType.Items.AddRange(new string[] { "Фарби", "Лицювальний матеріал", "Цемент", "Інше" });
            cmbSearchType.Items.AddRange(new string[] { "Фарби", "Лицювальний матеріал", "Цемент", "Інше" });

            // Додаємо опцію "Все" для пошуку по всіх типах
            cmbSearchType.Items.Insert(0, "Все");
            cmbSearchType.SelectedIndex = 0; // Встановлюємо "Все" як вибране за замовчуванням

            LoadProductsFromFile();  // Завантажуємо товари з файлу
            RefreshDataGrid(products); // Оновлюємо таблицю для відображення товарів
        }

        // Обробник кліку на кнопку "Додати товар"
        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            // Зчитуємо дані з полів вводу
            string name = txtName.Text.Trim();
            string type = cmbType.Text;
            string manufacturer = txtManufacturer.Text.Trim();
            bool isValidQty = int.TryParse(txtQuantity.Text, out int quantity);  // Перевірка коректності введеної кількості
            bool isValidPrice = decimal.TryParse(txtPrice.Text, out decimal price); // Перевірка коректності введеної ціни

            // Перевіряємо, щоб усі поля були заповнені коректно
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(manufacturer) || !isValidQty || !isValidPrice)
            {
                MessageBox.Show("Будь ласка, заповніть всі поля коректно.");
                return; // При некоректних даних - вихід без додавання
            }

            // Створюємо новий об'єкт товару
            var product = new Product
            {
                Name = name,
                Type = type,
                Manufacturer = manufacturer,
                Quantity = quantity,
                Price = price
            };

            // Додаємо товар у список
            products.Add(product);

            SaveProductsToFile();   // Зберігаємо оновлений список у файл
            RefreshDataGrid(products); // Оновлюємо відображення

            // Очищаємо поля вводу для зручності користувача
            txtName.Clear();
            cmbType.SelectedIndex = -1;
            txtManufacturer.Clear();
            txtQuantity.Clear();
            txtPrice.Clear();
        }

        // Обробник кліку на кнопку "Пошук"
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchType = cmbSearchType.Text;

            List<Product> filtered;

            if (searchType == "Все")
            {
                filtered = products; // Якщо вибрано "Все" - показуємо всі товари
            }
            else
            {
                // Фільтруємо товари за типом і сортуємо за ціною за зростанням
                filtered = products
                    .Where(p => p.Type == searchType)
                    .OrderBy(p => p.Price)
                    .ToList();
            }

            RefreshDataGrid(filtered); // Оновлюємо таблицю відфільтрованими товарами
        }

        // Перевірка наявності товару (по полях назва, тип, виробник)
        private void btnCheckAvailability_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string type = cmbType.Text;
            string manufacturer = txtManufacturer.Text.Trim();

            foreach (var p in products)
            {
                if (p.Name == name && p.Type == type && p.Manufacturer == manufacturer)
                {
                    MessageBox.Show("Товар знайдено на складі.");
                    return; // Якщо товар знайдено - показуємо повідомлення і виходимо
                }
            }

            MessageBox.Show("Товар не знайдено."); // Якщо товар не знайдено
        }

        // Постачання (додавання кількості товару)
        private void btnDeliver_Click(object sender, EventArgs e)
        {
            // Перевірка чи вибраний товар у таблиці
            if (dataGridViewProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Виберіть товар для постачання.");
                return;
            }

            var selectedIndex = dataGridViewProducts.SelectedRows[0].Index;
            int? toAdd = ShowInputDialog("Введіть кількість для постачання:", "Постачання");

            if (toAdd.HasValue)
            {
                // Збільшуємо кількість товару у списку
                products[selectedIndex].Quantity += toAdd.Value;
                SaveProductsToFile();   // Зберігаємо зміни
                RefreshDataGrid(products); // Оновлюємо відображення
            }
        }

        // Відпуск товару (зменшення кількості)
        private void btnWithdraw_Click(object sender, EventArgs e)
        {
            if (dataGridViewProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Виберіть товар для відпуску.");
                return;
            }

            var selectedIndex = dataGridViewProducts.SelectedRows[0].Index;
            int currentQty = products[selectedIndex].Quantity;

            int? toRemove = ShowInputDialog("Скільки одиниць відпустити?", "Відпуск товару");

            if (toRemove.HasValue)
            {
                if (toRemove.Value <= currentQty)
                {
                    // Віднімаємо від кількості вибране число
                    products[selectedIndex].Quantity -= toRemove.Value;
                    SaveProductsToFile();   // Зберігаємо
                    RefreshDataGrid(products); // Оновлюємо таблицю
                }
                else
                {
                    MessageBox.Show("Недостатньо товару."); // Якщо введена кількість більша за наявну
                }
            }
        }

        // Оновити дані з файлу (кнопка Оновити)
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadProductsFromFile();      // Заново зчитати товари з файлу
            RefreshDataGrid(products);   // Оновити таблицю
            MessageBox.Show("Оновлено з файлу."); // Повідомлення користувачу
        }

        // Оновлення DataGridView - очищаємо і додаємо рядки з товарами
        private void RefreshDataGrid(List<Product> data)
        {
            dataGridViewProducts.Rows.Clear(); // Очищуємо поточні рядки

            foreach (var p in data)
            {
                // Додаємо новий рядок з властивостями товару
                dataGridViewProducts.Rows.Add(p.Name, p.Type, p.Manufacturer, p.Quantity, p.Price);
            }
        }

        // Збереження списку товарів у файл
        private void SaveProductsToFile()
        {
            try
            {
                // Записуємо кожен товар у рядок через ToString()
                File.WriteAllLines(FilePath, products.Select(p => p.ToString()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка збереження: " + ex.Message); // Виводимо помилку, якщо щось пішло не так
            }
        }

        // Завантаження товарів з файлу у список
        private void LoadProductsFromFile()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var lines = File.ReadAllLines(FilePath); // Читаємо всі рядки з файлу
                    products = lines.Select(Product.FromString).ToList(); // Конвертуємо рядки у товари
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка читання: " + ex.Message); // Повідомлення при помилці читання
            }
        }

        // Діалог для вводу числового значення (використовується для постачання/відпуску)
        private int? ShowInputDialog(string text, string caption, string defaultValue = "1")
        {
            // Створюємо форму для вводу
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            // Додаємо підпис із поясненням
            Label textLabel = new Label() { Left = 10, Top = 10, Text = text, Width = 260 };
            // Поле для вводу
            TextBox inputBox = new TextBox() { Left = 10, Top = 35, Width = 260, Text = defaultValue };
            // Кнопка підтвердження
            Button confirmation = new Button() { Text = "OK", Left = 180, Width = 90, Top = 70, DialogResult = DialogResult.OK };

            // Додаємо контролі на форму
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation; // Нажаття Enter викликає кнопку OK

            // Якщо користувач натиснув OK
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                // Перевіряємо чи ввели коректне число більше 0
                if (int.TryParse(inputBox.Text, out int value) && value > 0)
                    return value;
                else
                    MessageBox.Show("Некоректне значення."); // Попередження, якщо некоректно
            }
            return null; // Якщо відмінили або некоректні дані
        }

        // Діалог для вводу тексту (використовується для редагування товару)
        private string ShowInputDialogString(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label textLabel = new Label() { Left = 10, Top = 10, Text = text, Width = 260 };
            TextBox inputBox = new TextBox() { Left = 10, Top = 35, Width = 260, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 180, Width = 90, Top = 70, DialogResult = DialogResult.OK };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            // Повертаємо текст, якщо користувач підтвердив, інакше null
            return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : null;
        }

        // Обробник кнопки "Редагувати товар"
        private void btnEditProduct_Click(object sender, EventArgs e)
        {
            // Перевіряємо, чи вибраний товар у таблиці
            if (dataGridViewProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Виберіть товар для редагування.");
                return;
            }

            int selectedIndex = dataGridViewProducts.SelectedRows[0].Index;
            var product = products[selectedIndex];

            // Запитуємо нові значення через діалогові вікна
            string newName = ShowInputDialogString("Назва товару:", "Редагування", product.Name);
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Назва не може бути порожньою.");
                return;
            }

            string newType = ShowInputDialogString("Тип товару:", "Редагування", product.Type);
            if (string.IsNullOrWhiteSpace(newType))
            {
                MessageBox.Show("Тип не може бути порожнім.");
                return;
            }

            string newManufacturer = ShowInputDialogString("Виробник:", "Редагування", product.Manufacturer);
            if (string.IsNullOrWhiteSpace(newManufacturer))
            {
                MessageBox.Show("Виробник не може бути порожнім.");
                return;
            }

            string qtyStr = ShowInputDialogString("Кількість:", "Редагування", product.Quantity.ToString());
            if (!int.TryParse(qtyStr, out int newQuantity) || newQuantity < 0)
            {
                MessageBox.Show("Некоректна кількість.");
                return;
            }

            string priceStr = ShowInputDialogString("Ціна:", "Редагування", product.Price.ToString());
            if (!decimal.TryParse(priceStr, out decimal newPrice) || newPrice < 0)
            {
                MessageBox.Show("Некоректна ціна.");
                return;
            }

            // Оновлюємо поля товару
            product.Name = newName;
            product.Type = newType;
            product.Manufacturer = newManufacturer;
            product.Quantity = newQuantity;
            product.Price = newPrice;

            SaveProductsToFile();   // Зберігаємо зміни
            RefreshDataGrid(products); // Оновлюємо відображення
        }

        // Обробник кнопки "Видалити товар"
        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            // Перевіряємо, чи вибраний товар у таблиці
            if (dataGridViewProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Виберіть товар для видалення.");
                return;
            }

            int selectedIndex = dataGridViewProducts.SelectedRows[0].Index;

            var productToDelete = products[selectedIndex];

            // Підтвердження дії видалення
            var result = MessageBox.Show($"Ви впевнені, що хочете видалити товар \"{productToDelete.Name}\"?",
                                         "Підтвердження видалення", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                products.RemoveAt(selectedIndex); // Видаляємо товар зі списку
                SaveProductsToFile();   // Зберігаємо зміни
                RefreshDataGrid(products); // Оновлюємо таблицю
            }
        }

        // Заглушки для незадіяних обробників подій (щоб не було помилок)
        private void cmbSearchType_SelectedIndexChanged(object sender, EventArgs e) { }
        private void cmbType_SelectedIndexChanged(object sender, EventArgs e) { }
        private void dataGridViewProducts_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void txtManufacturer_TextChanged(object sender, EventArgs e) { }
        private void txtName_TextChanged(object sender, EventArgs e) { }
        private void txtPrice_TextChanged(object sender, EventArgs e) { }
    }
}
