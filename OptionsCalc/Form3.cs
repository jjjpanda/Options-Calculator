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
        double strike, price;
        bool isCall, isLong;
        public Form3(double x, double price, bool isCall, bool isLong)
        {
            InitializeComponent();
            strike = x;
            this.price = price;
            this.isCall = isCall;
            this.isLong = isLong;
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            if (isCall)
            {
                double[] xValues = new double[4] { strike + (price * -1), strike + (price * -0), strike + (price * 1), strike + (price * 2) };
                if (isLong)
                {
                    double[] yValues = new double[4] {-1*price, -1*price, 0, price};
                    chart1.Series[0].Points.DataBindXY(xValues, yValues);
                }
                else
                {
                    double[] yValues = new double[4] {price, price, 0, -1*price};
                    chart1.Series[0].Points.DataBindXY(xValues, yValues);
                }
            }
            else
            {
                double[] xValues = new double[4] { strike + (price * -2), strike + (price * -1), strike + (price * 0), strike + (price * 1) };
                if (isLong)
                {
                   double[] yValues = new double[4] { price, 0, -1*price, -1*price};
                    chart1.Series[0].Points.DataBindXY(xValues, yValues);
                }
                else
                {
                    double[] yValues = new double[4] { -1*price, 0, price, price};
                    chart1.Series[0].Points.DataBindXY(xValues, yValues);
                }

            }

        }
    }
}
