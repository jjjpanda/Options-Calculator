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
    public partial class Form3 : Form
    {
        double[] xData, yData, greeks;

        public Form3(double[] x, double[] y, double[] greeks, string title)
        { 
            InitializeComponent();
            xData = x;
            yData = y;
            this.greeks = greeks;
            this.Text = title;
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chart1.Series[0].Points.DataBindXY(xData, yData);
            richTextBox1.Text = printGreeks(greeks);
        }

        private String printGreeks(double[] arr)
        {
            return "Delta: " + arr[0] + "\n" + "Gamma: " + arr[1] + "\n" + "Theta: " + arr[2] + "\n" + "Vega: " + arr[3] + "\n" + "Rho: " + arr[4] + "\n" + "IV: " + arr[5] + "\n";
        }
    }
}
