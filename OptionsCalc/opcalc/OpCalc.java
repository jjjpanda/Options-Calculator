import java.text.DecimalFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Arrays;
import java.util.Calendar;
import java.util.Date;
import java.util.Scanner;
import java.util.concurrent.TimeUnit;

public class OpCalc {
	
	public static Date parseDate(String date) {
		if(date.length() > 8) {
		     try {
		         return new SimpleDateFormat("MM/dd/yyyy").parse(date);
		     } catch (ParseException e) {
		         return null;
		     }
		}
		else{
		     try {
		         return new SimpleDateFormat("MM/dd/yy").parse(date);
		     } catch (ParseException e) {
		    	 try {
			         return new SimpleDateFormat("MM/dd").parse(date);
			     } catch (ParseException e2) {
			    	 try {
				         return new SimpleDateFormat("M/dd").parse(date);
				     } catch (ParseException e3) {
				    	 try {
					         return new SimpleDateFormat("MM/d").parse(date);
					     } catch (ParseException e4) {
					    	 try {
						         return new SimpleDateFormat("M/d").parse(date);
						     } catch (ParseException e5) {
						         return null;
						     }
					     }
				     }
			     }
		     }
		}
	  }

	public static void printGrid(double[][] a)
	{
	   DecimalFormat f = new DecimalFormat("##.00");
	   for(int i = 0; i < a.length; i++)
	   {
	      for(int j = 0; j < a[i].length; j++)
	      {
	         System.out.print(f.format(a[i][j])+ "  ");
	      }
	      System.out.println();
	   }
	}
	
	static double CNDF(double x)
	{
	    int neg = (x < 0d) ? 1 : 0;
	    if ( neg == 1) 
	        x *= -1d;

	    double k = (1d / ( 1d + 0.2316419 * x));
	    double y = (((( 1.330274429 * k - 1.821255978) * k + 1.781477937) *
	                   k - 0.356563782) * k + 0.319381530) * k;
	    y = 1.0 - 0.398942280401 * Math.exp(-0.5 * x * x) * y;

	    return (1d - neg) * y + neg * (1d - y);
	}
	
	static double NDF(double x) {
		return 1 / Math.sqrt(2*Math.PI) * Math.exp(-1 * x * x / 2); 
	}
	
	static double loss(double a, double b) {
		return Math.sqrt((a - b) * (a - b));
	}
	public static double getDifferenceDays(Date d1, Date d2) {
	    long diff = d2.getTime() - d1.getTime();
	    return (TimeUnit.DAYS.convert(diff, TimeUnit.MILLISECONDS)+1);
	}
	
	public static double d1(double p, double x, double t, double q, double r, double sigma) {
		return (Math.log(p/x) + t*(r- q + (sigma * sigma)/2))/(sigma*Math.sqrt(t));
	}
	
	public static double d2(double p, double x, double t, double q, double r, double sigma) {
		return (Math.log(p/x) + t*(r- q + (sigma * sigma)/2))/(sigma*Math.sqrt(t)) - (sigma*Math.sqrt(t));
	}
	
	public static double rfir(double p, double x, double t, double q, double trueIV, double priceOfOption) {
		double pcalc = 0;
		double r = 0.03;
		while(loss(priceOfOption, pcalc) > 0.000025) {
			pcalc = p*Math.exp(-1*q*t)*CNDF(d1(p,x,t,q,r,trueIV))-x*Math.exp(-1*r*t)*CNDF(d2(p,x,t,q,r,trueIV));
			if(priceOfOption > pcalc) {
				r*=1.2;
			}
			if(priceOfOption < pcalc) {
				r*=0.8;
			}
		}
		System.out.println(r);
		return r;
	}
	
	public static void main(String[] args) throws InterruptedException {
		Scanner console = new Scanner(System.in);
		
		System.out.println("Enter Underlying (Stock) Price: ");
		double p = console.nextDouble();
		int range = 21;
		double percentInterval = 1.005;
		
		System.out.println("Enter Div Yield: ");
		double q = console.nextDouble();
		q/=100;
		
		System.out.println("Enter Strike: ");
		double x = console.nextDouble();
		
		System.out.println("Call or Put? ");
		String typeOfOption;
		Object isCall = null;
		while(isCall == null) {
			typeOfOption = console.next();
			if(typeOfOption.equalsIgnoreCase("c") || typeOfOption.equalsIgnoreCase("call")) {
				isCall = true;
			}
			if(typeOfOption.equalsIgnoreCase("p") || typeOfOption.equalsIgnoreCase("put")){
				isCall = false;
			}
		}
		
		System.out.println("Enter Price: ");
		double priceOfOption = console.nextDouble();
		
		System.out.println("Enter Expiry: ");
		String date = console.next();
		Date expiry = parseDate(date);
		while(expiry == null) {
			System.out.println("Invalid Date ");
			date = console.next();
			expiry = parseDate(date);
		}
		Date today = Calendar.getInstance().getTime();
		if (expiry.getYear() < today.getYear()) {
			expiry.setYear(today.getYear());
		}
		double t = getDifferenceDays(today, expiry);
	
		double[][] profit = new double[range][ ((int)t) + 2];
		for (double[] row: profit) {
			Arrays.fill(row, -1*priceOfOption);	
		}
		
		for(int i = 0; i < profit.length; i++) {
			profit[i][0] = p*Math.pow(percentInterval, (range-1)/2)*Math.pow(1/percentInterval, i);
		}
		
		t/=365;
		
		double r = 0.05;
		//r = rfir(p,x,t,q,0.2593,priceOfOption);
		
		double iv = 0.20;
		double pcalc = 0;
		while(loss(priceOfOption, pcalc) > 0.000025) {
			pcalc = p*Math.exp(-1*q*t)*CNDF(d1(p,x,t,q,r,iv))-x*Math.exp(-1*r*t)*CNDF(d2(p,x,t,q,r,iv));
			if(priceOfOption > pcalc) {
				iv*=1.2;
			}
			if(priceOfOption < pcalc) {
				iv*=0.8;
			}
		}
		
		double delta = Math.exp(-1*q*t)*CNDF(d1(p,x,t,q,r,iv));
		double gamma = Math.exp(-1*q*t)*NDF(d1(p,x,t,q,r,iv))/(p*iv*Math.sqrt(t));
		double theta = (-(NDF(d1(p,x,t,q,r,iv))/(2*Math.sqrt(t)) * p*iv *Math.exp(-1*q*t))+
				(q*p*Math.exp(-1*q*t)*CNDF(d1(p,x,t,q,r,iv)))
				)/365;
		double vega = p/100*Math.exp(-1*q*t)*Math.sqrt(t)*NDF(d1(p,x,t,q,r,iv));
		double rho = t/100*x*CNDF(d2(p,x,t,q,r,iv));
		
		System.out.println("Estimates\n-------------------");
		System.out.println("IV: "+ iv);
		System.out.println("Delta: "+ delta);
		System.out.println("Gamma: "+ gamma);
		System.out.println("Theta: "+ theta);
		System.out.println("Vega: "+ vega);
		System.out.println("Rho: "+ rho);
		
		
		for(int i = 0; i < profit.length; i++) {
			double underlying = profit[i][0];
			for(int j = 1; j < profit[i].length; j++) {
				double daysLeft = (profit[i].length - j)/365.0;
				profit[i][j] += underlying*Math.exp(-1*q*daysLeft)*CNDF(d1(underlying,x,daysLeft,q,r,iv))-
						x*Math.exp(-1*r*daysLeft)*CNDF(d2(underlying,x,daysLeft,q,r,iv));
			}
		}
	
		printGrid(profit);
		
		console.close();
	}
}