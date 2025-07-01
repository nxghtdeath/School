using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
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

namespace SchoolApp
{ //SchoolDBConnection
    /// <summary>
    /// Логика взаимодействия для RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["SchoolDBConnection"].ConnectionString;

        public RegisterWindow()
        {
            InitializeComponent();
            LoadRoles();
            LoadClasses(); // Загружаем классы из БД
            LoadSubjects(); // Загружаем предметы из БД
        }

        private void LoadRoles()
        {
            RoleComboBox.Items.Clear();
            RoleComboBox.Items.Add("Ученик");
            RoleComboBox.Items.Add("Учитель");
            RoleComboBox.SelectedIndex = 0;
        }

        private void LoadClasses()
        {
            ClassComboBox.Items.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT ID_Класса, Номер FROM Классы";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        ClassComboBox.Items.Add(new ClassItem
                        {
                            ID_Класса = Convert.ToInt32(reader["ID_Класса"]),
                            Номер = reader["Номер"].ToString()
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки классов: " + ex.Message);
                }
            }
        }

        private void LoadSubjects()
        {
            SubjectComboBox.Items.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT ID_Предмета, Название FROM Предметы";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        SubjectComboBox.Items.Add(new SubjectItem
                        {
                            ID_Предмета = Convert.ToInt32(reader["ID_Предмета"]),
                            Название = reader["Название"].ToString()
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки предметов: " + ex.Message);
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();
            string surname = SurnameTextBox.Text.Trim();
            string name = NameTextBox.Text.Trim();
            string patronymic = PatronymicTextBox.Text.Trim();

            if (!BirthDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату рождения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DateTime birthDate = BirthDatePicker.SelectedDate.Value;

            string role = RoleComboBox.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(surname) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(role))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? selectedClassId = null;
            int? selectedSubjectId = null;

            if (role == "Ученик")
            {
                var selectedItem = ClassComboBox.SelectedItem as ClassItem;
                if (selectedItem == null)
                {
                    MessageBox.Show("Выберите класс для ученика.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                selectedClassId = selectedItem.ID_Класса;
            }
            else if (role == "Учитель")
            {
                var selectedItem = SubjectComboBox.SelectedItem as SubjectItem;
                if (selectedItem == null)
                {
                    MessageBox.Show("Выберите предмет для учителя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                selectedSubjectId = selectedItem.ID_Предмета;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Шаг 1: Добавляем пользователя
                    string userQuery = "INSERT INTO Пользователи (Логин, Пароль, Роль) VALUES (@login, @password, @role); SELECT SCOPE_IDENTITY();";
                    SqlCommand userCmd = new SqlCommand(userQuery, conn, transaction);
                    userCmd.Parameters.AddWithValue("@login", login);
                    userCmd.Parameters.AddWithValue("@password", password); // ⚠️ В реальном проекте пароль нужно хэшировать!
                    userCmd.Parameters.AddWithValue("@role", role);

                    int userId = Convert.ToInt32(userCmd.ExecuteScalar());

                    // Шаг 2: Если это ученик — добавляем в таблицу Ученики с указанием класса
                    if (role == "Ученик")
                    {
                        string studentQuery = @"INSERT INTO Ученики 
                                               (ID_Пользователя, Фамилия, Имя, Отчество, ДатаРождения, ID_Класса) 
                                                VALUES 
                                               (@userId, @surname, @name, @patronymic, @birthDate, @classId)";
                        SqlCommand studentCmd = new SqlCommand(studentQuery, conn, transaction);
                        studentCmd.Parameters.AddWithValue("@userId", userId);
                        studentCmd.Parameters.AddWithValue("@surname", surname);
                        studentCmd.Parameters.AddWithValue("@name", name);
                        studentCmd.Parameters.AddWithValue("@patronymic", patronymic ?? "");
                        studentCmd.Parameters.AddWithValue("@birthDate", birthDate);
                        studentCmd.Parameters.AddWithValue("@classId", selectedClassId.Value);

                        studentCmd.ExecuteNonQuery();
                    }
                    // Шаг 3: Если это учитель — добавляем в таблицу Учителя с указанием предмета
                    else if (role == "Учитель")
                    {
                        string teacherQuery = @"INSERT INTO Учителя 
                                              (ID_Пользователя, ID_Предмета, Фамилия, Имя, Отчество) 
                                               VALUES 
                                              (@userId, @subjectId, @surname, @name, @patronymic)";
                        SqlCommand teacherCmd = new SqlCommand(teacherQuery, conn, transaction);
                        teacherCmd.Parameters.AddWithValue("@userId", userId);
                        teacherCmd.Parameters.AddWithValue("@subjectId", selectedSubjectId.Value);
                        teacherCmd.Parameters.AddWithValue("@surname", surname);
                        teacherCmd.Parameters.AddWithValue("@name", name);
                        teacherCmd.Parameters.AddWithValue("@patronymic", patronymic ?? "");

                        teacherCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    // Вспомогательный класс для работы с ComboBox (классы)
    public class ClassItem
    {
        public int ID_Класса { get; set; }
        public string Номер { get; set; }

        public override string ToString()
        {
            return Номер;
        }
    }

    // Вспомогательный класс для работы с ComboBox (предметы)
    public class SubjectItem
    {
        public int ID_Предмета { get; set; }
        public string Название { get; set; }

        public override string ToString()
        {
            return Название;
        }
    }
}
