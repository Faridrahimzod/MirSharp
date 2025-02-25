using MirSharp;
using System.Collections.Generic;
using System.Windows.Forms;
using System;

namespace MirSharp
{
    public class HistoryForm : Form
    {
        private DataGridView dataGridView;

        public HistoryForm(List<CheckHistoryEntry> history)
        {
            // Настройка формы
            this.Text = "История проверок";
            this.Size = new System.Drawing.Size(800, 600);

            // Создаем DataGridView
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };

            // Настраиваем столбцы вручную
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

            // Добавляем кнопку для просмотра полного текста
            var viewFullTextButton = new Button
            {
                Text = "Просмотреть полный текст",
                Dock = DockStyle.Bottom
            };
            viewFullTextButton.Click += ViewFullTextButton_Click;
            this.Controls.Add(viewFullTextButton);
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
    }
}