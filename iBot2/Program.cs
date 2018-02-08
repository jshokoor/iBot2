using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Globalization;

namespace iBot2
{
    class Program
    {
        static void Main(string[] args)
        {
            SQLiteConnection.CreateFile("MyDatabase.sqlite");
            SQLiteConnection m_dbConnection;
            m_dbConnection =
                new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            m_dbConnection.Open();

            //Sql table creation, only needs to be executed the first time the application is run.
            string sqlCreate = "create table WorkersCompensationFees (ndcNumber varchar(12), labelName varchar(50), priceDateStart varchar(12), numberOfUnits varchar(12), unitPrice varChar(50), product int, totalOfIngredients varchar(50), mediCalDispensingFee varchar(20), subtotal varchar(20), priceComparison varchar(20), paymentPrice varchar(20))";
            SQLiteCommand commandCreate = new SQLiteCommand(sqlCreate, m_dbConnection);
            commandCreate.ExecuteNonQuery();

            //date of service needs to use current date, rather than hard-coded date.
            String nDCNumber = "55111068405";
            String metricDecimalNumberOfUnits = "50";
            String usualAndCustomaryPrice = "$13.12";
            String dateOfServiceInput = "1/31/2018";
            //setup some variables end

            String result = "";
            String strPost = "NDCno="+nDCNumber+"&MDUnits="+metricDecimalNumberOfUnits+"&PriceBilled="+usualAndCustomaryPrice+"&DateOfService="+dateOfServiceInput;
            StreamWriter myWriter = null;

            HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create("https://www.dir.ca.gov/dwc/pharmfeesched/pfs.asp");
            objRequest.Method = "POST";
            objRequest.ContentLength = strPost.Length;
            objRequest.ContentType = "application/x-www-form-urlencoded";

            try
            {
                myWriter = new StreamWriter(objRequest.GetRequestStream());
                myWriter.Write(strPost);
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
            }
            finally {
                myWriter.Close();
            }

            HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
            using (StreamReader sr = 
                new StreamReader(objResponse.GetResponseStream()) )
            {
                result = sr.ReadToEnd();
                
                // Close and clean up the StreamReader
                sr.Close();
            }

            //variables necessary for parsing HTML response
            List<string> arrayOfCleanValue = new List<string>();
            List<string> myTables = new List<string>(result.Split(new string[] { "class=\"tabborder\"" }, StringSplitOptions.None));
            List<string> myRows = new List<string>(myTables[1].Split(new string[] { "<tr" }, StringSplitOptions.None));
            List<string> itemsInSecondRow = new List<string>(myRows[2].Split(new string[] { "<td>" }, StringSplitOptions.None));
            List<string> secondaryTemp = new List<string>();
            string thirdTemp = "";

            for (int x=0; x < itemsInSecondRow.Count(); x++)
            {
                List<string> temp = new List<string>(itemsInSecondRow[x].Split(new string[] { "\r\n" }, StringSplitOptions.None));
                secondaryTemp.Add(temp[1]);
            }

            for (int y = 1; y < secondaryTemp.Count(); y++)
            {
                thirdTemp = secondaryTemp[y].Trim();
                arrayOfCleanValue.Add(thirdTemp);
            }

            //variables necessary for calculations to determine total prices
            double usualAndCustomaryPriceAsNumber = 13.12;
            double totalOfIngredientsAsNumber = 0;
            string totalOfIngredients = "";
            double medicalDispensingFeeAsNumber = 7.25;
            string medicalDispensingFee = "$7.25";
            double subtotalAsNumber = 0;
            string subtotal = "";
            string finalPrice = "";

            //calcuting subtotal, then payment price is determined by comparison to customary price
            totalOfIngredientsAsNumber = Convert.ToDouble(arrayOfCleanValue[arrayOfCleanValue.Count - 1]);
            totalOfIngredients = "$" + arrayOfCleanValue[arrayOfCleanValue.Count - 1];
            arrayOfCleanValue.Add(totalOfIngredients);
            arrayOfCleanValue.Add(medicalDispensingFee);
            subtotalAsNumber = totalOfIngredientsAsNumber + medicalDispensingFeeAsNumber;
            subtotal = Convert.ToString(subtotalAsNumber);
            subtotal = "$" + subtotal;
            arrayOfCleanValue.Add(subtotal);
            arrayOfCleanValue.Add(usualAndCustomaryPrice);
            if (usualAndCustomaryPriceAsNumber < subtotalAsNumber)
            {
                finalPrice = usualAndCustomaryPrice;
            }
            else
            {
                finalPrice = subtotal;
            }
            arrayOfCleanValue.Add(finalPrice);

            //instantiating remaining variables to output to db
            string labelName = arrayOfCleanValue[1];
            string priceDateStart = arrayOfCleanValue[2];
            string unitPrice = arrayOfCleanValue[4];
            string product = arrayOfCleanValue[6];
            
            //output data to sql table
            string sqlInsert = $"insert into WorkersCompensationFees (ndcNumber, labelName, priceDateStart, numberOfUnits, unitPrice, product, totalOfIngredients, mediCalDispensingFee, subtotal, priceComparison, paymentPrice) values ('{nDCNumber}', '{labelName}', '{priceDateStart}', '{metricDecimalNumberOfUnits}', '{unitPrice}', '{subtotalAsNumber}', '{product}', '{medicalDispensingFee}', '{subtotal}', '{usualAndCustomaryPrice}', '{totalOfIngredients}')";
            SQLiteCommand commandInsert = new SQLiteCommand(sqlInsert, m_dbConnection);
            commandInsert.ExecuteNonQuery();

            //sql query below to view the outputted data
            /*string sqlSelect = "select * from WorkersCompensationFees order by ndcNumber desc";
            SQLiteCommand commandSelect = new SQLiteCommand(sqlSelect, m_dbConnection);
            SQLiteDataReader sqlReader = commandSelect.ExecuteReader();
            /*while (reader.Read())
                System.Diagnostics.Debug.WriteLine("Name: " + reader["name"] + "\tScore: " + reader["score"]);
            */

            m_dbConnection.Close();
        }
    }
}
