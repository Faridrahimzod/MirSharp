using MirSharp;
using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.IO;

namespace MirSharp
{
    public class HistoryForm : Form
    {
        private DataGridView dataGridView;
        private List<CheckHistoryEntry> _history;

        public HistoryForm(List<CheckHistoryEntry> history)
        {
            _history = history;

            this.Text = "История проверок";
            this.Size = new System.Drawing.Size(800, 600);

            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };

            // Настраиваем столбцы
            dataGridView.Columns.Add("FileName", "Имя файла");
            dataGridView.Columns.Add("CheckDate", "Дата проверки");
            dataGridView.Columns.Add("Result", "Результат");

            // Настраиваем столбец Result для переноса текста
            dataGridView.Columns["Result"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // Заполняем DataGridView данными
            foreach (var entry in history)
            {
                dataGridView.Rows.Add(entry.FileName, entry.CheckDate, entry.Result);
            }

            // Добавляем DataGridView на форму
            this.Controls.Add(dataGridView);

            


            var panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            var clearButton = new Button
            {
                Text = "Очистить историю",
                Width = 120,
                Dock = DockStyle.Left
            };
            clearButton.Click += ClearButton_Click;

            // Существующая кнопка просмотра
            var viewButton = new Button
            {
                Text = "Просмотреть полный текст",
                Width = 180,
                Dock = DockStyle.Right
            };
            viewButton.Click += ViewFullTextButton_Click;

            panel.Controls.Add(clearButton);
            panel.Controls.Add(viewButton);
            this.Controls.Add(panel);
        }

        private void ViewFullTextButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow != null)
            {
                string fullText = dataGridView.CurrentRow.Cells["Result"].Value?.ToString();
                if (!string.IsNullOrEmpty(fullText))
                {
                    MessageBox.Show(fullText, "Полный текст результата", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Результат отсутствует.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите полностью очистить историю?",
                                       "Подтверждение",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var checker = new HistoryChecking(Path.Combine(Directory.GetCurrentDirectory(), "DataBase.db"));
                checker.ClearHistory();
                _history.Clear();
                dataGridView.Rows.Clear();
                MessageBox.Show("История успешно очищена!");
            }
        }
    }
}