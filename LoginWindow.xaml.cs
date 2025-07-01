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
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["SchoolDBConnection"].ConnectionString;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Шаг 1: Получаем ID_Пользователя и Роль
                    int? userId = null;
                    string role = null;

                    using (SqlCommand cmd = new SqlCommand("SELECT ID_Пользователя, Роль FROM Пользователи WHERE Логин = @login AND Пароль = @password", conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userId = Convert.ToInt32(reader["ID_Пользователя"]);
                                role = reader["Роль"].ToString();
                            }
                        } // SqlDataReader автоматически закрывается
                    } // SqlCommand тоже освобождается

                    if (!userId.HasValue)
                    {
                        MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Шаг 2: Если это ученик — получаем ID_Ученика
                    if (role == "Ученик")
                    {
                        int studentId = GetStudentId(conn, userId.Value);
                        if (studentId == -1)
                        {
                            MessageBox.Show("Не удалось найти данные об ученике.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        StudentWindow studentWindow = new StudentWindow(studentId);
                        studentWindow.Show();
                        this.Close();
                    }
                    else if (role == "Учитель")
                    {
                        TeacherWindow teacherWindow = new TeacherWindow(userId.Value);
                        teacherWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Неизвестная роль пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }

        // Получает ID_Ученика по ID_Пользователя
        private int GetStudentId(SqlConnection conn, int userId)
        {
            string query = "SELECT ID_Ученика FROM Ученики WHERE ID_Пользователя = @userId";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@userId", userId);

                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return -1;

                return Convert.ToInt32(result);
            }
        }
    }
}
