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
using ClosedXML.Excel;
using System.Diagnostics;



namespace MirSharp
{
    public partial class Form1 : Form
    {
        List<string> files_names = new List<string>() { }; 
        string pathInTextBox1;
        string result = "";
        private HistoryChecking _historyRepository;
        string dataPath = Path.Combine(Directory.GetCurrentDirectory(), "DataBase.db");
        public Form1()
        {
            InitializeComponent();
            this.Visible = true; 
            this.WindowState = FormWindowState.Normal;

            _historyRepository = new HistoryChecking(dataPath);
            
            button1.AllowDrop = true;
            button1.DragEnter += new DragEventHandler(button1_DragEnter);
            button1.DragDrop += new DragEventHandler(button1_DragDrop);

            panel1.Controls.Add(textBox2);

            textBox2.Multiline = true;
            textBox2.ScrollBars = ScrollBars.Vertical;

            textBox2.MouseWheel += new MouseEventHandler(textBox2_MouseWheel);
            listBoxFiles.MouseMove += ListBoxFiles_MouseMove;


        }
        private void ListBoxFiles_MouseMove(object sender, MouseEventArgs e)
        {
            int index = listBoxFiles.IndexFromPoint(e.Location);
            if (index >= 0 && index < files_names.Count)
            {
                toolTip.SetToolTip(listBoxFiles, files_names[index]);
            }
            else
            {
                toolTip.Hide(listBoxFiles);
            }
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
                    if (!IsCsFile(path) && !IsProjectFolder(path))
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
            UpdateFilesList();


        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            pathInTextBox1 = textBox1.Text;
            UpdateFilesList();

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
            UpdateFilesList();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (files_names.Count == 0)
            {
                MessageBox.Show("Не выбрано ни одного файла для анализа!");
                return;
            }
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

            try
            {
                // Создаем один анализатор для всех файлов
                CodeStyler codeStyler = new CodeStyler(files_names);

                // Получаем результаты анализа
                result = codeStyler.ErrorAnalyzer();
                result += codeStyler.StyleAnalyzer();

                // Сохраняем результат для каждого файла
                foreach (var file in files_names)
                {
                    _historyRepository.AddCheckResult(file, result);
                }

                // Показываем результаты
                panel1.Visible = true;
                textBox2.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка анализа: {ex.Message}");
            }
            finally
            {
                // Очищаем список файлов после анализа
                files_names.Clear();
                UpdateFilesList();
            }

            
            
        }
        
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void textBox2_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                SendKeys.Send("{UP}"); 
            }
            else if (e.Delta < 0)
            {
                SendKeys.Send("{DOWN}"); 
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

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(result))
            {
                MessageBox.Show("Нет данных для экспорта!");
                return;
            }

            if (cmbFileFormat.SelectedItem == null)
            {
                MessageBox.Show("Выберите формат файла!");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            switch (cmbFileFormat.SelectedItem.ToString())
            {
                case "Text File (.txt)":
                    saveFileDialog.Filter = "Text files (*.txt)|*.txt";
                    break;
                case "CSV File (.csv)":
                    saveFileDialog.Filter = "CSV files (*.csv)|*.csv";
                    break;
                case "Excel File (.xls)":
                    saveFileDialog.Filter = "Excel files (*.xls)|*.xls";
                    break;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = saveFileDialog.FileName;

                    switch (Path.GetExtension(filePath).ToLower())
                    {
                        case ".txt":
                            ExportToTxt(filePath);
                            break;
                        case ".csv":
                            ExportToCsv(filePath);
                            break;
                        case ".xls":
                            ExportToExcel(filePath);
                            break;
                    }

                    MessageBox.Show("Экспорт завершен успешно!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                }
            }
        }

        private void ExportToTxt(string path)
        {
            File.WriteAllText(path, result);
        }

        private void ExportToCsv(string path)
        {
            StringBuilder csv = new StringBuilder();

            csv.AppendLine("Тип;Описание;Строка");

            foreach (var line in result.Split('\n'))
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ':' }, 2);
                    csv.AppendLine($"Ошибка;{parts[1].Trim()};{parts[0].Trim()}");
                }
            }

            File.WriteAllText(path, csv.ToString(), Encoding.UTF8);
        }

        private void ExportToExcel(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    MessageBox.Show("Не указан путь для сохранения файла!");
                    return;
                }

                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xlsx");

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Результаты анализа");

                    worksheet.Cell(1, 1).Value = "Тип";
                    worksheet.Cell(1, 2).Value = "Описание";
                    worksheet.Cell(1, 3).Value = "Строка";

                    string[] lines = result.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    int row = 2;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (line.Contains(":"))
                        {
                            var parts = line.Split(new[] { ':' }, 2);
                            worksheet.Cell(row, 1).Value = "Ошибка";
                            worksheet.Cell(row, 2).Value = parts[1].Trim();
                            worksheet.Cell(row, 3).Value = parts[0].Trim();
                        }
                        else
                        {
                            worksheet.Cell(row, 1).Value = line.Trim();
                            worksheet.Range(row, 1, row, 3).Merge();
                        }
                        row++;
                    }

                    workbook.SaveAs(tempFilePath);
                }

                File.Copy(tempFilePath, filePath, overwrite: true);

                if (File.Exists(filePath))
                {
                    MessageBox.Show($"Файл успешно сохранен:\n{filePath}", "Готово",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
                else
                {
                    MessageBox.Show("Файл не был создан по неизвестной причине", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет прав для записи в указанную директорию", "Ошибка доступа",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте:\n{ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateFilesList()
        {
            listBoxFiles.Items.Clear();
            foreach (var file in files_names)
            {
                listBoxFiles.Items.Add(Path.GetFileName(file));
            }

        }
    }
}
