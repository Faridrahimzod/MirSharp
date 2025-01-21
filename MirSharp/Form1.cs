using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO.Compression;


namespace MirSharp
{
    public partial class Form1 : Form
    {
        List<string> files_names = new List<string>() { };
        string pathInTextBox1;
        public Form1()
        {
            InitializeComponent();
            // Включаем поддержку перетаскивания
            button1.AllowDrop = true;

            // Обработка события, когда файл перетаскивается над TextBox
            button1.DragEnter += new DragEventHandler(button1_DragEnter);

            // Обработка события, когда файл отпускается над TextBox
            button1.DragDrop += new DragEventHandler(button1_DragDrop);

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //Вспомогательные методы для проверки
        // Проверяем, что путь ведёт к .cs файлу
        private bool IsCsFile(string path)
        {
            return File.Exists(path) && Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase);
        }

        // Проверяем, что путь ведёт к папке с проектом (содержит .csproj или .sln файл)
        private bool IsProjectFolder(string path)
        {
            if (Directory.Exists(path))
            {
                // Проверяем наличие .csproj или .sln файлов в папке
                return Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0 ||
                       Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Length > 0;
            }
            return false;
        }
        private bool IsArchive(string path)
        {
            return File.Exists(path) && Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        }
        private void ProcessFolder(string folderPath)
        {
            // Добавляем все .cs файлы из папки
            string[] csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
            files_names.AddRange(csFiles);

            // Ищем папки с проектами (содержащие .csproj или .sln файлы)
            foreach (string subFolder in Directory.GetDirectories(folderPath))
            {
                if (IsProjectFolder(subFolder))
                {
                    string[] projectCsFiles = Directory.GetFiles(subFolder, "*.cs", SearchOption.AllDirectories);
                    files_names.AddRange(projectCsFiles);
                }
            }
        }

        private void button1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Проверяем, что все элементы — это .cs файлы, папки с проектами или архивы
                bool allValid = true;
                foreach (string path in files)
                {
                    if (!IsCsFile(path) && !IsProjectFolder(path) && !IsArchive(path))
                    {
                        allValid = false;
                        break;
                    }
                }

                if (allValid)
                {
                    e.Effect = DragDropEffects.Copy; // Разрешаем перетаскивание
                }
                else
                {
                    e.Effect = DragDropEffects.None; // Запрещаем перетаскивание
                }
            }
            else
            {
                e.Effect = DragDropEffects.None; // Запрещаем перетаскивание
            }
        }

        private void button1_DragDrop(object sender, DragEventArgs e)
        {
            // Получаем массив путей к файлам
            string[] file = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string s in file)
            {
                if (IsCsFile(s))
                {
                    files_names.Add(s);
                }
                else if (IsProjectFolder(s))
                {
                    string[] csFiles = Directory.GetFiles(s, "*.cs", SearchOption.AllDirectories);
                    foreach (string csFile in csFiles)
                    {
                        files_names.Add((string)csFile);
                    }
                }
                else if (IsArchive(s))
                {
                    // Если это архив, распаковываем его и проверяем содержимое
                    string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    ZipFile.ExtractToDirectory(s, extractPath);

                    // Ищем .cs файлы и проекты в распакованной папке
                    ProcessFolder(extractPath);

                    // Удаляем временную папку после использования (опционально)
                    Directory.Delete(extractPath, true);
                }
            }

            
            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            pathInTextBox1 = textBox1.Text;
                        
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            // Создаем экземпляр OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Настраиваем диалог
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.Filter = "C# файлы (*.cs)|*.cs|C# проекты (*.csproj;*.sln)|*.csproj;*.sln|Архивные файлы (*.zip)|*.zip|Все файлы (*.*)|*.*";
            openFileDialog.Multiselect = true; // Разрешаем выбор нескольких файлов
            openFileDialog.Title = "Выберите файлы"; // Заголовок диалога

            // Открываем диалог и проверяем, нажал ли пользователь "ОК"
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Получаем массив выбранных файлов
                string[] selectedFiles = openFileDialog.FileNames;

                // Выводим пути к выбранным файлам (например, в MessageBox)
                foreach (string file in selectedFiles)
                {
                    if (IsCsFile(file))
                    {
                        files_names.Add(file);
                    }
                    else if (IsProjectFolder(file))
                    {
                        string[] csFiles = Directory.GetFiles(file, "*.cs", SearchOption.AllDirectories);
                        foreach (string csFile in csFiles)
                        {
                            files_names.Add((string)csFile);
                        }
                    }
                    else if (IsArchive(file))
                    {
                        // Если это архив, распаковываем его и проверяем содержимое
                        string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        ZipFile.ExtractToDirectory(file, extractPath);

                        // Ищем .cs файлы и проекты в распакованной папке
                        ProcessFolder(extractPath);

                        // Удаляем временную папку после использования (опционально)
                        Directory.Delete(extractPath, true);
                    }
                }

                // Если нужно отобразить пути в TextBox или ListBox:
                // listBoxFiles.Items.AddRange(selectedFiles);
                // textBoxFiles.Text = string.Join(Environment.NewLine, selectedFiles);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (pathInTextBox1 != null)
            {
                try
                {
                    string path = Path.GetDirectoryName(pathInTextBox1);
                    if (IsCsFile(path))
                    {
                        files_names.Add(path);
                    }
                    else if (IsProjectFolder(path))
                    {
                        string[] csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                        foreach (string csFile in csFiles)
                        {
                            files_names.Add((string)csFile);
                        }
                    }
                    else if (IsArchive(path))
                    {
                        // Если это архив, распаковываем его и проверяем содержимое
                        string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        ZipFile.ExtractToDirectory(path, extractPath);

                        // Ищем .cs файлы и проекты в распакованной папке
                        ProcessFolder(extractPath);

                        // Удаляем временную папку после использования (опционально)
                        Directory.Delete(extractPath, true);
                    }
                    textBox1.Clear();
                }
                catch (Exception)
                {
                    MessageBox.Show("Введён неправильный путь к файлам, повторите попытку");
                    textBox1.Clear();
                }
            }

            //Основная часть кода для проверки на кодстайл
            

        }
    }
}
