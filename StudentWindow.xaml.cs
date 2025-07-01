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
    /// Логика взаимодействия для StudentWindow.xaml
    /// </summary>
    public partial class StudentWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["SchoolDBConnection"].ConnectionString;
        private int studentId;

        public StudentWindow(int studentId)
        {
            InitializeComponent();
            this.studentId = studentId;
            LoadStudentInfo();
            LoadSchedule();
            LoadGrades();
        }

        private void LoadStudentInfo()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT Фамилия, Имя, Отчество, ДатаРождения FROM Ученики WHERE ID_Ученика = @id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", studentId);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string fullName = $"{reader["Фамилия"]} {reader["Имя"]} {reader["Отчество"]}";
                        string birthDate = Convert.ToDateTime(reader["ДатаРождения"]).ToShortDateString();

                        StudentInfoTextBlock.Text = $"ФИО: {fullName}\nДата рождения: {birthDate}";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки информации о студенте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadSchedule()
        {
            List<ScheduleItem> scheduleItems = new List<ScheduleItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Шаг 1: Получаем ID класса
                    string queryGetClassId = "SELECT ID_Класса FROM Ученики WHERE ID_Ученика = @id";
                    SqlCommand cmdGetClassId = new SqlCommand(queryGetClassId, conn);
                    cmdGetClassId.Parameters.AddWithValue("@id", studentId);

                    object result = cmdGetClassId.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        MessageBox.Show("Для этого ученика не назначен класс.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int classId = Convert.ToInt32(result);

                    // Шаг 2: Загружаем расписание по классу
                    string query = "SELECT r.ДеньНедели, p.Название AS Предмет, r.Кабинет " +
                                  "FROM Расписание r " +
                                  "JOIN Предметы p ON r.ID_Предмета = p.ID_Предмета " +
                                  "WHERE r.ID_Класса = @classId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@classId", classId);

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        ScheduleItem item = new ScheduleItem
                        {
                            ДеньНедели = reader["ДеньНедели"].ToString(),
                            Предмет = reader["Предмет"].ToString(),
                            Кабинет = reader["Кабинет"].ToString()
                        };
                        scheduleItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки расписания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            ScheduleDataGrid.ItemsSource = scheduleItems;
        }

        private void LoadGrades()
        {
            List<GradeItem> gradeItems = new List<GradeItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT p.Название AS Предмет, o.Оценка " +
                                  "FROM Оценки o " +
                                  "JOIN Предметы p ON o.ID_Предмета = p.ID_Предмета " +
                                  "WHERE o.ID_Ученика = @id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", studentId);

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        GradeItem item = new GradeItem
                        {
                            Предмет = reader["Предмет"].ToString(),
                            Оценка = Convert.ToInt32(reader["Оценка"])
                        };
                        gradeItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки оценок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            GradesDataGrid.ItemsSource = gradeItems;
        }
    }

    // Модель для расписания
    public class ScheduleItem
    {
        public string ДеньНедели { get; set; }
        public string Предмет { get; set; }
        public string Кабинет { get; set; }
    }

    // Модель для оценок
    public class GradeItem
    {
        public string Предмет { get; set; }
        public int Оценка { get; set; }
    }
}
