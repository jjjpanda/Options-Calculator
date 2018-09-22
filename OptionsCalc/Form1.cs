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

namespace OptionsCalc
{
    public partial class Form1 : Form
    {
        int comments, sumNumOfContracts;

        int range;
        double percentInterval = 1.05;

        double priceUnderlying, divYield = 0, r = 0;
        double x;
        double priceOfOption;
        
        bool isLong;
        bool isCall;
        List<double[,]> mergedProfitList = new List<double[,]>();
        double mergedEntryCost = 0;

        double[] greeks = new double[6];

        ToolTip tt = new ToolTip();

        public Form1()
        {
            InitializeComponent();
            textBox5.ContextMenu = new ContextMenu();
        }

        static double[,] Copy(double[,] x)
        {
            double[,] copy = new double[x.GetLength(0), x.GetLength(1)];
            for(int i = 0; i < x.GetLength(0); i++)
            {
                for(int j = 0; j < x.GetLength(1); j++)
                {
                    copy[i, j] = x[i, j];
                }
            }
            return copy;
        }

        static void Error(string msg)
        {
            MessageBox.Show(msg, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static double CNDF(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }

        static double NDF(double x)
        {
            return 1 / Math.Sqrt(2 * Math.PI) * Math.Exp(-1 * x * x / 2);
        }

        static double Loss(double a, double b)
        {
            return Math.Sqrt((a - b) * (a - b));
        }

        static double D1(double p, double x, double t, double q, double r, double sigma)
        {
            return (Math.Log(p / x) + t * (r - q + (sigma * sigma) / 2)) / (sigma * Math.Sqrt(t));
        }

        static double D2(double p, double x, double t, double q, double r, double sigma)
        {
            return (Math.Log(p / x) + t * (r - q + (sigma * sigma) / 2)) / (sigma * Math.Sqrt(t)) - (sigma * Math.Sqrt(t));
        }

        private void button1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                textBox5.Visible = true;
            }

            if(e.Button == MouseButtons.Left)
            {
                textBox5.Visible = false;
                String input = richTextBox1.Text;
                String[] lines = input.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                //
                //IV CRUSH check
                //
                bool doesIVChange = false;
                DateTime[] IVdates = new DateTime[2];
                int rangeOfIVChange = 0;
                double IVchangeRate = 0;
                String[] IVdata = textBox5.Text.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (IVdata[0].Equals("IV"))
                {
                    if (IVdata.Length == 3)
                    {
                        string[] dates = IVdata[1].Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (dates.Length == 1 || dates.Length == 2)
                        {
                            rangeOfIVChange = dates.Length;
                            for (int i = 0; i < dates.Length; i++)
                            {
                                try
                                {
                                    IVdates[i] = Convert.ToDateTime(dates[i]);
                                }
                                catch
                                {
                                    Error("Invalid IV Change Input");
                                }
                            }
                            try 
                            {
                                IVchangeRate = Convert.ToDouble(IVdata[2]);
                                if (IVchangeRate < 0)
                                {
                                    Error("Cannot have negative IV change: \nExample: If you want a decrease of 30%, type 0.7");
                                }
                                else
                                {
                                    doesIVChange = true;
                                }

                            }
                            catch
                            {
                                Error("Invalid IV Change Input");
                            }
                        }
                        else
                        {
                            Error("Invalid IV Change Input");
                        }
                    }
                }
                else if(textBox5.Text == "")
                {
                    //Do nothing, I suppose.
                }
                else
                {
                    Error("Invalid IV Change Input");
                }

                //
                // Check all options written in text box
                //
                comments = 0;
                for (int a = 0; a < lines.Length; a++)
                {
                    if (lines[a].Substring(0, 2).Equals("//"))
                    {
                        comments++;
                        continue;
                    }
                    String[] data = lines[a].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (data.Length <= 1)
                    {
                        continue;
                    }
                    int count = 0;

                    //
                    //Date
                    //
                    DateTime expiry;
                    try
                    {
                        expiry = Convert.ToDateTime(data[count]);
                    }
                    catch
                    {
                        Error("Invalid");
                        return;
                    }
                    DateTime today = DateTime.Today;
                    int daysLeft = (expiry - today).Days;
                    double t = daysLeft / 365.0;
                    if (t < 0)
                    {
                        Error("Invalid Date");
                        return;
                    }
                    count++;

                    //
                    //Strike
                    //
                    try
                    {
                        x = Convert.ToDouble(data[count]);
                    }
                    catch
                    {
                        Error("Invalid Strike Price");
                        return;
                    }
                    count++;

                    //
                    //Call/Put
                    //
                    try
                    {
                        if (data[count].Equals("Call", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("C", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isCall = true;
                        }
                        else if (data[count].Equals("Put", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("P", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isCall = false;
                        }
                        else
                        {
                            Error("Invalid, Enter Call, C, Put or P");
                            return;
                        }
                    }
                    catch
                    {
                        return;
                    }
                    count++;

                    //
                    // Buy or Sell
                    //
                    try
                    {
                        if (data[count].Equals("Long", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("Buy", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("L", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isLong = true;
                        }
                        else if (data[count].Equals("Short", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("Sell", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("Write", StringComparison.InvariantCultureIgnoreCase) || data[count].Equals("S", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isLong = false;
                        }
                        else
                        {
                            Error("Invalid, Enter Long, Short, Buy, Sell, or Write");
                            return;
                        }
                    }
                    catch
                    {
                        return;
                    }
                    count++;

                    //
                    //Price
                    //
                    try
                    {
                        priceOfOption = Convert.ToDouble(data[count]);
                    }
                    catch
                    {
                        Error("Invalid Option Price");
                        return;
                    }
                    count++;

                    //
                    // Number of Contracts or Bought at Price
                    //
                    int numOfContracts = 1;
                    double optionBoughtAt = 0.0;
                    if (data.Length > count && !data[count].Equals(""))
                    {
                        try
                        {
                            if (data[count][0] == 'x' || data[count][0] == 'X' || data[count][0] == '*')
                            {
                                numOfContracts = Convert.ToInt16(data[count].Substring(1));
                            }
                            else if (data[count][0] == '@')
                            {
                                optionBoughtAt = Convert.ToDouble(data[count].Substring(1));
                            }
                            else
                            {
                                Error("Invalid");
                                return;
                            }
                        }
                        catch
                        {
                            Error("Invalid");
                            return;
                        }
                    }
                    if (optionBoughtAt == 0)
                    {
                        optionBoughtAt = priceOfOption;
                    }
                    count++;

                    //
                    // Num of Contracts if not handled yet
                    //
                    if (data.Length > count && !data[count].Equals(""))
                    {
                        try
                        {
                            if (data[count][0] == 'x' || data[count][0] == 'X' || data[count][0] == '*')
                            {
                                numOfContracts = Convert.ToInt16(data[count].Substring(1));
                            }
                            else
                            {
                                Error("Invalid Multiple");
                                return;
                            }
                        }
                        catch
                        {
                            Error("Invalid Multiple");
                            return;
                        }
                    }
                    if(numOfContracts < 1)
                    {
                        continue;
                    }
                    sumNumOfContracts += numOfContracts;

                    //
                    // div yield and interest rate
                    //
                    try
                    {
                        priceUnderlying = Convert.ToDouble(textBox1.Text);
                        divYield = 0;
                        if (!textBox2.Text.Equals("") && !textBox2.Text.Contains("\r") && !textBox2.Text.Contains("\n"))
                        {
                            divYield = Convert.ToDouble(textBox2.Text) / 100;
                        }
                        r = 0;
                        if (!textBox4.Text.Equals("") && !textBox4.Text.Contains("\r") && !textBox4.Text.Contains("\n"))
                        {
                            r = Convert.ToDouble(textBox4.Text) / 100;
                        }
                    }
                    catch
                    {
                        Error("Invalid Stock Price, Div Yield, or Interest Rate");
                        return;
                    }

                    //
                    // Add price of option to Arrays
                    //
                    range = Convert.ToInt16(numericUpDown1.Value);
                    double[,] profit = new double[range, daysLeft + 3];
                    for (int i = 0; i < profit.GetLength(0); i++)
                    {
                        for (int j = 0; j < profit.GetLength(1) - 1; j++)
                        {
                            if (isLong)
                            {
                                profit[i, j] = -1 * numOfContracts * optionBoughtAt;
                            }
                            else if (!isLong)
                            {
                                profit[i, j] = numOfContracts * optionBoughtAt;
                            }
                        }
                    }
                    if (isLong)
                    {
                        mergedEntryCost -= numOfContracts * optionBoughtAt;
                    }
                    else if (!isLong)
                    {
                        mergedEntryCost += numOfContracts * optionBoughtAt;
                    }

                    //
                    //% Interval
                    //
                    try
                    {
                        percentInterval = Convert.ToDouble(textBox3.Text) / 100 + 1;
                    }
                    catch
                    {
                        Error("Invalid Percent Interval");
                        return;
                    }
                    for (int i = 0; i < profit.GetLength(0); i++)
                    {
                        profit[i, 0] = priceUnderlying * Math.Pow(percentInterval, (range - 1) / 2) * Math.Pow(1 / percentInterval, i);
                    }

                    //
                    // Calculate IV
                    //
                    double iv = 0.20;
                    double priceOfOptionTheoretical = 0;
                    while (Loss(priceOfOption, priceOfOptionTheoretical) > 0.00025)
                    {
                        if (isCall)
                        {
                            priceOfOptionTheoretical = priceUnderlying * Math.Exp(-1 * divYield * t) * CNDF(D1(priceUnderlying, x, t, divYield, r, iv)) - x * Math.Exp(-1 * r * t) * CNDF(D2(priceUnderlying, x, t, divYield, r, iv));
                        }
                        else if (!isCall)
                        {
                            priceOfOptionTheoretical = -1 * priceUnderlying * Math.Exp(-1 * divYield * t) * CNDF(-1 * D1(priceUnderlying, x, t, divYield, r, iv)) + x * Math.Exp(-1 * r * t) * CNDF(-1 * D2(priceUnderlying, x, t, divYield, r, iv));
                        }
                        if (priceOfOption > priceOfOptionTheoretical)
                        {
                            iv += 0.00001;
                        }
                        if (priceOfOption < priceOfOptionTheoretical)
                        {
                            iv -= 0.00001;
                        }
                    }

                    //
                    // IV Change Math
                    //
                    int IVchangeStart = 0, IVchangeEnd = 0;
                    if (doesIVChange)
                    {
                        IVchangeStart = (IVdates[0] - today).Days;
                        if (rangeOfIVChange == 2)
                        {
                            IVchangeEnd = (IVdates[1] - today).Days;
                        }
                    }
                    if ((rangeOfIVChange == 2 && IVchangeEnd < IVchangeStart) || IVchangeEnd < 0 || IVchangeStart < 0)
                    {
                        Error("Invalid IV Change Date");
                        doesIVChange = false;
                    }
                    IVchangeStart += 2;
                    IVchangeEnd += 2;
                    if (IVchangeRate < -1)
                    {
                        Error("Invalid IV Change Input");
                        doesIVChange = false;
                    }
                    if (rangeOfIVChange == 2)
                    {
                        IVchangeRate /= (IVchangeEnd - IVchangeStart);
                    }

                    //
                    //Calculate Greeks
                    //
                    double delta = 0, gamma = 0, theta = 0, vega = 0, rho = 0;
                    if (isCall)
                    {
                        delta = Math.Exp(-1 * divYield * t) * CNDF(D1(priceUnderlying, x, t, divYield, r, iv));
                        gamma = Math.Exp(-1 * divYield * t) * NDF(D1(priceUnderlying, x, t, divYield, r, iv)) / (priceUnderlying * iv * Math.Sqrt(t));
                        theta = (-(NDF(D1(priceUnderlying, x, t, divYield, r, iv)) / (2 * Math.Sqrt(t)) * priceUnderlying * iv * Math.Exp(-1 * divYield * t)) +
                                (divYield * priceUnderlying * Math.Exp(-1 * divYield * t) * CNDF(D1(priceUnderlying, x, t, divYield, r, iv))) -
                                (r * x * Math.Exp(-1 * r * t) * NDF(D2(priceUnderlying, x, t, divYield, r, iv)))
                                ) / 365;
                        vega = priceUnderlying / 100 * Math.Exp(-1 * divYield * t) * Math.Sqrt(t) * NDF(D1(priceUnderlying, x, t, divYield, r, iv));
                        rho = t / 100 * Math.Exp(-1 * r * t) * x * CNDF(D2(priceUnderlying, x, t, divYield, r, iv));
                    }
                    else if (!isCall)
                    {
                        delta = Math.Exp(-1 * divYield * t) * (CNDF(D1(priceUnderlying, x, t, divYield, r, iv)) - 1);
                        gamma = Math.Exp(-1 * divYield * t) * NDF(D1(priceUnderlying, x, t, divYield, r, iv)) / (priceUnderlying * iv * Math.Sqrt(t));
                        theta = (-(NDF(D1(priceUnderlying, x, t, divYield, r, iv)) / (2 * Math.Sqrt(t)) * priceUnderlying * iv * Math.Exp(-1 * divYield * t)) -
                                (divYield * priceUnderlying * Math.Exp(-1 * divYield * t) * CNDF(-1 * D1(priceUnderlying, x, t, divYield, r, iv))) -
                                (r * x * Math.Exp(-1 * r * t) * NDF(-1 * D2(priceUnderlying, x, t, divYield, r, iv)))
                                ) / 365;
                        vega = priceUnderlying / 100 * Math.Exp(-1 * divYield * t) * Math.Sqrt(t) * NDF(D1(priceUnderlying, x, t, divYield, r, iv));
                        rho = t / -100 * Math.Exp(-1 * r * t) * x * CNDF(-1 * D2(priceUnderlying, x, t, divYield, r, iv));
                    }
                    if (!isLong)
                    {
                        delta *= -1;
                        gamma *= -1;
                        theta *= -1;
                        vega *= -1;
                        rho *= -1;
                    }
                    greeks[0] += delta;
                    greeks[1] += gamma;
                    greeks[2] += theta;
                    greeks[3] += vega;
                    greeks[4] += rho;
                    greeks[5] += iv * numOfContracts;

                    //
                    // Calculate Profit
                    //
                    for (int i = 0; i < profit.GetLength(0); i++)
                    {
                        double underlying = profit[i, 0];
                        for (int j = 1; j < profit.GetLength(1) - 1; j++)
                        {
                            double tPrime = ((profit.GetLength(1) - j)-2) / 365.0;
                         
                            if (doesIVChange && rangeOfIVChange == 1 && j >= IVchangeStart)
                            {
                                double ivprime = iv * (IVchangeRate);
                                if (isCall)
                                {
                                    if (isLong)
                                    {
                                        profit[i, j] += numOfContracts * (underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(D1(underlying, x, tPrime, divYield, r, ivprime)) -
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }
                                    else if (!isLong)
                                    {
                                        profit[i, j] -= numOfContracts * (underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(D1(underlying, x, tPrime, divYield, r, ivprime)) -
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }

                                }
                                else if (!isCall)
                                {
                                    if (isLong)
                                    {
                                        profit[i, j] += numOfContracts * (-1 * underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(-1 * D1(underlying, x, tPrime, divYield, r, ivprime)) +
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(-1 * D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }
                                    else if (!isLong)
                                    {
                                        profit[i, j] -= numOfContracts * (-1 * underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(-1 * D1(underlying, x, tPrime, divYield, r, ivprime)) +
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(-1 * D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }
                                }
                            }
                            else if (doesIVChange && rangeOfIVChange == 2 && j >= IVchangeStart)
                            {
                                double ivprime = iv * (IVchangeRate * (j - IVchangeStart));
                                if(j > IVchangeEnd)
                                {
                                    ivprime = iv * (1 + IVchangeRate * (IVchangeEnd - IVchangeStart));
                                }
                                if (isCall)
                                {
                                    if (isLong)
                                    {
                                        profit[i, j] += numOfContracts * (underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(D1(underlying, x, tPrime, divYield, r, ivprime)) -
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }
                                    else if (!isLong)
                                    {
                                        profit[i, j] -= numOfContracts * (underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(D1(underlying, x, tPrime, divYield, r, ivprime)) -
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }

                                }
                                else if (!isCall)
                                {
                                    if (isLong)
                                    {
                                        profit[i, j] += numOfContracts * (-1 * underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(-1 * D1(underlying, x, tPrime, divYield, r, ivprime)) +
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(-1 * D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }
                                    else if (!isLong)
                                    {
                                        profit[i, j] -= numOfContracts * (-1 * underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(-1 * D1(underlying, x, tPrime, divYield, r, ivprime)) +
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(-1 * D2(underlying, x, tPrime, divYield, r, ivprime)));
                                    }
                                }
                            }
                            else
                            {
                                if (isCall)
                                {
                                    if (isLong)
                                    {
                                        profit[i, j] += numOfContracts * (underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(D1(underlying, x, tPrime, divYield, r, iv)) -
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(D2(underlying, x, tPrime, divYield, r, iv)));
                                    }
                                    else if (!isLong)
                                    {
                                        profit[i, j] -= numOfContracts * (underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(D1(underlying, x, tPrime, divYield, r, iv)) -
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(D2(underlying, x, tPrime, divYield, r, iv)));
                                    }

                                }
                                else if (!isCall)
                                {
                                    if (isLong)
                                    {
                                        profit[i, j] += numOfContracts * (-1 * underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(-1 * D1(underlying, x, tPrime, divYield, r, iv)) +
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(-1 * D2(underlying, x, tPrime, divYield, r, iv)));
                                    }
                                    else if (!isLong)
                                    {
                                        profit[i, j] -= numOfContracts * (-1 * underlying * Math.Exp(-1 * divYield * tPrime) * CNDF(-1 * D1(underlying, x, tPrime, divYield, r, iv)) +
                                        x * Math.Exp(-1 * r * tPrime) * CNDF(-1 * D2(underlying, x, tPrime, divYield, r, iv)));
                                    }
                                }
                            }
                        }
                    }

                    //
                    //profit at expiry
                    //
                    for (int i = 0; i < profit.GetLength(0); i++)
                    {
                        double underlying = profit[i, 0];
                        int j = profit.GetLength(1) - 1;
                        if (isCall)
                        {
                            if (isLong)
                            {
                                profit[i, j] = numOfContracts * Math.Max(((-1 * optionBoughtAt) + (underlying - x)), (-1 * optionBoughtAt));
                            }
                            else if (!isLong)
                            {
                                profit[i, j] = numOfContracts * Math.Min((optionBoughtAt - (underlying - x)), optionBoughtAt);                             
                            }

                        }
                        else if (!isCall)
                        {
                            if (isLong)
                            { 
                                profit[i, j] = numOfContracts * Math.Max(((-1 * optionBoughtAt) + (-1 * underlying + x)), (-1 * optionBoughtAt));
                            }
                            else if (!isLong)
                            {
                                profit[i, j] = numOfContracts * Math.Min((optionBoughtAt - (-1 * underlying + x)), optionBoughtAt);
                            }
                        }
                    }

                    //
                    //print profit
                    //
                    double[,] roundedProfit = Copy(profit);
                    for (int i = 0; i < roundedProfit.GetLength(0); i++)
                    {
                        for (int j = 0; j < roundedProfit.GetLength(1); j++)
                        {
                            if (j > 0)
                            {
                                if (!checkBox2.Checked)
                                {
                                    roundedProfit[i, j] /= (numOfContracts * optionBoughtAt / 100);
                                }
                                else
                                {
                                    roundedProfit[i, j] /= (optionBoughtAt / (Math.Abs(optionBoughtAt)) / 100);
                                }
                            }
                            roundedProfit[i, j] = Math.Round(roundedProfit[i, j], 2);
                            profit[i, j] = Math.Round(profit[i, j], 2);
                        }
                    }

                    mergedProfitList.Add(profit);

                    if (checkBox1.Checked || lines.Length <= 1)
                    {
                        Form2 form2 = new Form2(roundedProfit, today, lines[a]);
                        form2.Show();
                        if (checkBox3.Checked)
                        {
                            Form3 form3 = new Form3(GetColumn(roundedProfit, 0), GetColumn(roundedProfit, roundedProfit.GetLength(1) - 1), new double[] { delta, gamma, theta, vega, rho, iv }, lines[a]);
                            form3.Show();
                        }
                    }
                }

                //
                // Merged options calculations
                //
                if (richTextBox1.Text != "")
                {
                    int minimum = 0;
                    minimum = mergedProfitList.Min(profit => profit.GetLength(1));
                    double[,] mergedProfit = new double[range, minimum];
                    for (int i = 0; i < range; i++)
                    {
                        for (int j = 1; j < minimum; j++)
                        {
                            for (int a = 0; a < mergedProfitList.Count; a++)
                            {
                                mergedProfit[i, j] += mergedProfitList.ElementAt(a)[i, j];
                                mergedProfit[i, 0] = mergedProfitList.ElementAt(a)[i, 0];
                            }
                        }
                    }
                    Console.WriteLine(mergedProfit[20, 20]);
                    for (int i = 0; i < mergedProfit.GetLength(0); i++)
                    {
                        for (int j = 1; j < mergedProfit.GetLength(1); j++)
                        {
                            if (!checkBox2.Checked) 
                            {
                                if(mergedEntryCost != 0)
                                {
                                    mergedProfit[i, j] /= (Math.Abs(mergedEntryCost) / 100);
                                }
                                else
                                {
                                    mergedProfit[i, j] /= (0.0001 / 100);
                                }
                            }
                            else
                            {
                                mergedProfit[i, j] *= 100;
                            }
                            mergedProfit[i, j] = Math.Round(mergedProfit[i, j], 2);
                        }
                    }
                    Console.WriteLine(mergedProfit[20, 20]);
                    if (lines.Length > 1)
                    {
                        Form2 form2merged;
                        if (mergedEntryCost < 0)
                        {
                            form2merged = new Form2(mergedProfit, DateTime.Today, "Strategy, Entry Debit: " + -1 * mergedEntryCost);
                        }
                        else if (mergedEntryCost > 0)
                        {
                            form2merged = new Form2(mergedProfit, DateTime.Today, "Strategy, Entry Credit: " + mergedEntryCost);
                        }
                        else
                        {
                            form2merged = new Form2(mergedProfit, DateTime.Today, "Strategy, Entry Net Cost: " + mergedEntryCost);
                        }
                        form2merged.Show();
                    }
                    if (checkBox3.Checked && lines.Length > 1)
                    {
                        greeks[5] /= sumNumOfContracts; 
                        Form3 form3 = new Form3(GetColumn(mergedProfit, 0), GetColumn(mergedProfit, mergedProfit.GetLength(1) - 1), greeks, "Strategy");
                        form3.Show();
                    }
                    greeks = new double[6];
                    mergedProfitList.Clear();
                    mergedEntryCost = 0;
                    sumNumOfContracts = 0;
                }
            }
        }

       private void richTextBox1_Hover(object sender, EventArgs e)
        {
            
            RichTextBox TB = (RichTextBox)sender;
            tt.Show("Example: 12/31 90 C Buy 1.93 x2 or 12/31 90 P Short 2.3 @3.41 X4", TB, TB.Width/2, TB.Height, 4000);
        }

        private void textBox5_Hover(object sender, EventArgs e)
        {
            TextBox TB = (TextBox)sender;
            tt.Show("Example: IV 9/21 0.9 or IV 9/21-9/27 1.3", TB, TB.Width, 0, 4000);
        }

        private void textBox5_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                textBox5.Clear();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*"
            };
            DialogResult open = dialog.ShowDialog();
            if (open == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, textBox1.Text.ToString() + Environment.NewLine +
                                           textBox2.Text.ToString() + Environment.NewLine
                                         + textBox4.Text.ToString() + Environment.NewLine +
                                            numericUpDown1.Value.ToString()
                                         + Environment.NewLine + textBox3.Text.ToString() + Environment.NewLine +
                                         textBox5.Text.ToString() + Environment.NewLine +
                                         richTextBox1.Text.ToString());
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*"
            };
            DialogResult open = dialog.ShowDialog();
            if (open == DialogResult.OK)
            {
                string text = File.ReadAllText(dialog.FileName);
                string[] linesOfText = text.Split('\n');
                try{ 
                    textBox1.Text = linesOfText[0];
                    textBox2.Text = linesOfText[1];
                    textBox4.Text = linesOfText[2];
                    numericUpDown1.Value = Convert.ToInt16(linesOfText[3]);
                    textBox3.Text = linesOfText[4];
                    textBox5.Text = linesOfText[5];
                    richTextBox1.Clear();
                    for(int i = 6; i < linesOfText.Length; i++)
                    {
                        richTextBox1.Text += linesOfText[i] + "\n";
                    }  
                }
                catch
                {
                    Error("Invalid File");
                    return;
                }
            }
        }

        public double[] GetColumn(double[,] matrix, int columnNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(0))
                    .Select(x => matrix[x, columnNumber])
                    .ToArray();
        }

        /*
        for (int i = 0; i < mergedProfit.GetLength(0); i++)
        {
            for (int j = 0; j < mergedProfit.GetLength(1); j++)
            {
                Console.Write(mergedProfit[i, j] + " ");
            }
            Console.Write(Environment.NewLine + Environment.NewLine);
        }
        Console.ReadLine();
        */
    }
}
