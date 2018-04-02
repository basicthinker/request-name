using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
//using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
//Using the imported service reference. You may have another name for the reference.
using econSOAPExample.econSoap;

using MailKit.Net.Smtp;
using MailKit;
using MimeKit;


namespace econSOAPExample
{
    class Program
    {
        static void Main(string[] args)
        {
            GetEmployeeData();
            EmailReportTLS();
        }

        private struct EmployeeDataRecord
        {
            public string name;
            public string email;
            public decimal hours;
            public decimal cost;
            public decimal sales;
            public decimal earned;
            public decimal invoicedKPI;
            public decimal productiveKPI;
        }

        private static List<EmployeeDataRecord> EmployeeDataRecords = new List<EmployeeDataRecord>();

        private static void Connect(EconomicWebServiceSoapClient session)
        {
            // A necessary setting as the session is put in a cookie
            ((BasicHttpBinding)session.Endpoint.Binding).AllowCookies = true;


            using (new OperationContextScope(session.InnerChannel))
            {
                //Setting the X-EconomicAppIdentifier HTTP Header. Only required for ConnectAsAdministrator.
                //var requestMessage = new HttpRequestMessageProperty();
                //requestMessage.Headers["X-EconomicAppIdentifier"] =
                //    "MyCoolIntegration/1.1 (http://example.com/MyCoolIntegration/; MyCoolIntegration@example.com) BasedOnSuperLib/1.4";
                //OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = requestMessage;

                // Connect as administrator
                //session.ConnectAsAdministrator(ADMINAGREEMENT, "ADMINUSER", "PASS", ENDUSERAGREEMENT);

                // Connect with token  
                session.ConnectWithToken("DCsTo8OiHmZOEi2z3nCL4UIGgTHEMFODyDhLgf6hBDY1", "uveDMwKpURdJ3HikednSkDd42OO83xSAJim2IhvWKuI1");
            }
        }

        public static void GetEmployeeData()
        {
            using (var session = new EconomicWebServiceSoapClient())
            {
                Console.WriteLine("Connecting");
                Connect(session);

                var companyHandle = session.Company_Get();
                var companyData = session.Company_GetData(companyHandle);

                Console.WriteLine(companyData.Name);

                //Fetch and loop employees
                var employeeHandles = session.Employee_GetAll();
                foreach (EmployeeHandle employee in employeeHandles)
                {
                    var employeeData = session.Employee_GetData(employee);
                    //Console.WriteLine(employeeData.Name + " CostPrice=" + employeeData.CostPrice + " SalesPrice=" + employeeData.SalesPrice);
                    var timeEntries = session.Employee_GetTimeEntries(employee);
                    var timeEntryDataArr = session.TimeEntry_GetDataArray(timeEntries);
                    decimal employeeHourSum = 0;
                    decimal employeeCostSum = 0;
                    decimal employeeSalesSum = 0;
                    decimal employeeEarnedSum = 0;
                    DateTime fromDate = DateTime.Parse("01.01.2011");
                    DateTime toDate = DateTime.Parse("31.12.2012");


                    //Loop time entries for the particular employee
                    foreach (TimeEntryData timeEntryData in timeEntryDataArr)
                    {
                        //Get activity data (To ananlyze productive or unproductive time)
                        //var timeActivityHandle = timeEntryData.ActivityHandle;
                        //var timeActivityData = session.Activity_GetData(timeActivityHandle);
                        //Console.WriteLine("Activity=" + timeActivityData.Name + " number=" + timeActivityData.Number);


                        //Filter on date interval
                        var res1 = timeEntryData.Date.CompareTo(fromDate);
                        var res2 = timeEntryData.Date.CompareTo(toDate);
                        if (res1 > 0 & res2 < 0)
                        {
                            //Sum data
                            employeeHourSum = employeeHourSum + timeEntryData.NumberOfHours;
                            employeeCostSum = employeeCostSum + timeEntryData.CostPrice * timeEntryData.NumberOfHours;
                            employeeSalesSum = employeeSalesSum + timeEntryData.SalesPrice * timeEntryData.NumberOfHours;
                            employeeEarnedSum = employeeSalesSum - employeeCostSum;

                        }

                        //Output individual timeentry data
                        //Console.WriteLine(timeEntryData.Date + ":" + timeEntryData.NumberOfHours + " " + timeEntryData.SalesPrice + " " + timeEntryData.Text);
                    }

                    Console.WriteLine(employeeData.Name + " \t TimeregHours=" + employeeHourSum + " \t Cost=" + employeeCostSum + " \t Sales=" + employeeSalesSum + " \t Earned=" + employeeEarnedSum);

                    //Add record to the list
                    EmployeeDataRecord temp;
                    temp.name = employeeData.Name;
                    temp.email = "ts@sinno.dk";
                    temp.hours = employeeHourSum;
                    temp.cost = employeeCostSum;
                    temp.sales = employeeSalesSum;
                    temp.earned = employeeEarnedSum;
                    temp.invoicedKPI = 0;
                    temp.productiveKPI = 0;
                    EmployeeDataRecords.Add(temp);
                }

                Console.WriteLine("Disconnecting");
                session.Disconnect();
                Console.WriteLine("Done - Press any key to proceed");
                Console.ReadKey();
            }
        }

        private static string PrepareEmailMessage()
        {
            string returnString;

            returnString = @"Hej,

Dette er din månedlige statusmail. Din udfaktureringsgrad har været 93% og din produktivitetsgrad har været på 100%.

Det lever fint op til forventningerne.

Mvh Thomas";

            return returnString;
        }

        public static void EmailReportTLS()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sinno Office", "office@sinno.dk"));
            message.To.Add(new MailboxAddress("Thomas Schmidt", "ts@sinno.dk"));
            message.Subject = "Månedsrapport";
            message.Body = new TextPart("plain") { Text = PrepareEmailMessage() };

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Connect using TLS (Office 365 requirement)
                Console.WriteLine("Connect to SMTP Server using TLS");
                client.Connect("smtp.office365.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                // Authenticate using login credentials
                Console.WriteLine("Authenticate Login");
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate("ts@sinno.dk", "");

                //Send email and disconnect from smtp-server
                Console.WriteLine("Send message");
                client.Send(message);
                client.Disconnect(true);

                Console.WriteLine("Done - Press any key to proceed");
                Console.ReadKey();
            }
        }
    }
}
