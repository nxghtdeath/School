using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
    /// Логика взаимодействия для TeacherWindow.xaml
    /// </summary>
    public partial class TeacherWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["SchoolDBConnection"].ConnectionString;
        private int teacherId; // ID учителя

        public TeacherWindow(int teacherId)
        {
            InitializeComponent();
            this.teacherId = teacherId;
            LoadSubjects(); // Загружаем предметы, которые ведёт этот учитель
        }

        // Загрузка предметов, которые ведёт этот учитель
        private void LoadSubjects()
        {
            SubjectComboBox.Items.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Получаем ID_Учителя по ID_Пользователя
                    string getTeacherQuery = "SELECT ID_Учителя FROM Учителя WHERE ID_Пользователя = @userId";
                    SqlCommand getTeacherCmd = new SqlCommand(getTeacherQuery, conn);
                    getTeacherCmd.Parameters.AddWithValue("@userId", teacherId);

                    object teacherIdObj = getTeacherCmd.ExecuteScalar();

                    if (teacherIdObj == null)
                    {
                        MessageBox.Show("Для данного пользователя не найден учитель.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int teacherIdFromDb = Convert.ToInt32(teacherIdObj);

                    // Загружаем предметы для данного учителя
                    string query = @"SELECT s.Название 
                                     FROM Предметы s
                                     JOIN Учителя t ON s.ID_Предмета = t.ID_Предмета
                                     WHERE t.ID_Учителя = @teacherId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@teacherId", teacherIdFromDb);

                    Console.WriteLine($"Debug: Выполняется запрос: {query}");
                    Console.WriteLine($"Debug: параметр @teacherId = {teacherIdFromDb}");

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        MessageBox.Show("Для данного учителя нет назначенных предметов.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    while (reader.Read())
                    {
                        Console.WriteLine($"Debug: Найден предмет - {reader["Название"]}");
                        SubjectComboBox.Items.Add(reader["Название"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки предметов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 🔄 При выборе предмета — загружаем оценки по нему
        private void SubjectComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SubjectComboBox.SelectedItem == null) return;
            LoadGrades();
        }

        // 📋 Загрузка оценок по выбранному предмету
        private void LoadGrades()
        {
            GradesListBox.Items.Clear();

            string subjectName = SubjectComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(subjectName)) return;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string query = @"SELECT st.Фамилия, st.Имя, g.Оценка, g.Комментарий
                                     FROM Оценки g
                                     JOIN Ученики st ON g.ID_Ученика = st.ID_Ученика
                                     JOIN Предметы sbj ON g.ID_Предмета = sbj.ID_Предмета
                                     WHERE sbj.Название = @subjectName";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@subjectName", subjectName);

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        string fullName = $"{reader["Фамилия"]} {reader["Имя"]}";
                        string grade = reader["Оценка"].ToString();
                        string comment = reader["Комментарий"] as string ?? "";

                        GradesListBox.Items.Add($"{fullName}: {grade} ({comment})");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки оценок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ➕ Добавление новой оценки
        private void AddGradeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StudentIdTextBox.Text) || SubjectComboBox.SelectedItem == null || string.IsNullOrWhiteSpace(GradeTextBox.Text))
            {
                MessageBox.Show("Введите ID ученика, выберите предмет и введите оценку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int studentId;
            if (!int.TryParse(StudentIdTextBox.Text.Trim(), out studentId))
            {
                MessageBox.Show("Неверный формат ID ученика.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string subjectName = SubjectComboBox.SelectedItem.ToString();
            string grade = GradeTextBox.Text.Trim();
            string comment = CommentTextBox.Text.Trim();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Получаем ID предмета по названию
                    string getSubjectIdQuery = "SELECT ID_Предмета FROM Предметы WHERE Название = @subjectName";
                    SqlCommand getSubjectIdCmd = new SqlCommand(getSubjectIdQuery, conn);
                    getSubjectIdCmd.Parameters.AddWithValue("@subjectName", subjectName);
                    object subjectIdObj = getSubjectIdCmd.ExecuteScalar();

                    if (subjectIdObj == null)
                    {
                        MessageBox.Show("Не удалось найти указанный предмет.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int subjectId = Convert.ToInt32(subjectIdObj);

                    // Вставляем оценку
                    string insertQuery = @"INSERT INTO Оценки (ID_Ученика, ID_Предмета, Оценка, Комментарий) 
                                           VALUES (@studentId, @subjectId, @grade, @comment)";
                    SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@studentId", studentId);
                    insertCmd.Parameters.AddWithValue("@subjectId", subjectId);
                    insertCmd.Parameters.AddWithValue("@grade", grade);
                    insertCmd.Parameters.AddWithValue("@comment", string.IsNullOrEmpty(comment) ? (object)DBNull.Value : comment);

                    insertCmd.ExecuteNonQuery();

                    LoadGrades();
                    MessageBox.Show("Оценка успешно добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления оценки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
