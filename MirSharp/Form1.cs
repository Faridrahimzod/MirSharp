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
        List<string> files_names = new List<string>() { }; // Список для хранения путей к файлам типа .cs
        string pathInTextBox1; // Строка-путь в textbox1
        string result = "";
        private HistoryChecking _historyRepository;
        string dataPath = Path.Combine(Directory.GetCurrentDirectory(), "DataBase.db");
        public Form1()
        {
            InitializeComponent();
            _historyRepository = new HistoryChecking(dataPath);
            
            // Включаем поддержку перетаскивания
            button1.AllowDrop = true;
            button1.DragEnter += new DragEventHandler(button1_DragEnter);
            button1.DragDrop += new DragEventHandler(button1_DragDrop);

            panel1.Controls.Add(textBox2);

            textBox2.Multiline = true;
            textBox2.ScrollBars = ScrollBars.Vertical;

            // Подписка на событие MouseWheel
            textBox2.MouseWheel += new MouseEventHandler(textBox2_MouseWheel);

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //Вспомогательные методы для проверки
        /// <summary>
        /// Проверяем, что путь ведёт к .cs файлу
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsCsFile(string path)
        {
            return File.Exists(path) && Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Проверяем, что путь ведёт к папке с проектом (содержит .csproj или .sln файл)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Проверяем, что путь ведёт к архиву
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
                    e.Effect = DragDropEffects.Copy; 
                }
                else
                {
                    e.Effect = DragDropEffects.None; 
                }
            }
            else
            {
                e.Effect = DragDropEffects.None; 
            }
        }

        private void button1_DragDrop(object sender, DragEventArgs e)
        {
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
                    string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    ZipFile.ExtractToDirectory(s, extractPath);

                    ProcessFolder(extractPath);
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
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.Filter = "C# файлы (*.cs)|*.cs|C# проекты (*.csproj;*.sln)|*.csproj;*.sln|Архивные файлы (*.zip)|*.zip|Все файлы (*.*)|*.*";
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Выберите файлы"; 

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] selectedFiles = openFileDialog.FileNames;

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
                        string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        ZipFile.ExtractToDirectory(file, extractPath);

                        ProcessFolder(extractPath);
                        Directory.Delete(extractPath, true);
                    }
                }

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
                        string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        ZipFile.ExtractToDirectory(path, extractPath);

                        ProcessFolder(extractPath);
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
            foreach (string file in files_names)
            {
                
                CodeStyler codeStyler = new CodeStyler(file);
                result += codeStyler.ErrorAnalyzer();
                result += "\n";
                result += codeStyler.StyleAnalyzer();
                
            }

            // Сохраняем результат в базу данных
            foreach (string file in files_names)
            {
                _historyRepository.AddCheckResult(file, result);
            }

            panel1.Visible = true;

            textBox2.Text = result;
            files_names.Clear();
            result = string.Empty;
            
        }
        
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void textBox2_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                SendKeys.Send("{UP}"); // Прокрутка вверх
            }
            else if (e.Delta < 0)
            {
                SendKeys.Send("{DOWN}"); // Прокрутка вниз
            }
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            

        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox2.Clear();
            panel1.Visible=false;
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var history = _historyRepository.GetCheckHistory();
            var historyForm = new HistoryForm(history);
            historyForm.ShowDialog();
        }
    }
}
