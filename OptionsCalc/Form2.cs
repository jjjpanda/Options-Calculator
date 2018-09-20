using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OptionsCalc
{
    public partial class Form2 : Form
    {
        double[,] data;
        DateTime today;

        private void PopulateDataGrid(double[,] myArr)
        {
            DataTable dataTable = new DataTable();
            for (int j = 0; j < myArr.GetLength(1); j++)
                dataTable.Columns.Add(new DataColumn("Column " + j.ToString()));

            for (int i = 0; i < myArr.GetLength(0); i++)
            {
                var newRow = dataTable.NewRow();
                for (int j = 0; j < myArr.GetLength(1); j++)
                {
                    newRow["Column " + j.ToString()] = myArr[i, j];
                }
                dataTable.Rows.Add(newRow);
            }
            this.dataGridView1.DataSource = dataTable;
            dataGridView1.Columns[0].Width = 40;
            dataGridView1.Columns[0].HeaderText = "Price";
            for (int k = 1; k < data.GetLength(0); k++)
            {
                dataGridView1.Rows[k].Height = 0;
            }
            for (int k = 1; k < data.GetLength(1); k++)
            {
                dataGridView1.Columns[k].Width = 40;
                dataGridView1.Columns[k].HeaderText = today.AddDays(k-1).ToShortDateString();
                dataGridView1.Columns[k].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            dataGridView1.Columns[0].Frozen = true;

        }

        public Form2(double[,] graph, DateTime today, string title)
        {
            InitializeComponent();
            data = graph;
            this.today = today;
            Text = title;
            PopulateDataGrid(graph);
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellUIChange(object sender, DataGridViewCellFormattingEventArgs e)
        {
            
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 1; j < data.GetLength(1); j++)
                {

                    double a = data[i, j];
                    int x = Convert.ToInt32(data[i, j]);
                    
                    if (x > 0)
                    {
                        if((100-x)*2 < 0)
                        {
                            x = 100;
                        }
                        this.dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.FromArgb(255, (100 - x) * 2, 255, (100 - x) * 2);
                    }
                    else if (x < 0)
                    { 
                        if((100+x)*2 < 0)
                        {
                            x = -100;
                        }
                        this.dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.FromArgb(255, 255, (100 + x) * 2, (100 + x) * 2);
                    }
                }
            }
            
        }
    }
}
