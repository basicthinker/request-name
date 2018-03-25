using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
//Using the imported service reference. You may have another name for the reference.
using econSOAPExample.econSoap;

namespace econSOAPExample
{
    class Program
    {
        static void Main(string[] args)
        {
            PrintCompanyName();
        }

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
                session.ConnectWithToken("rJiD2Hm8iD3iWG0xNGTQ1o7w7FuHrI2SH0Yiz1dH81E1", "uveDMwKpURdJ3HikednSkDd42OO83xSAJim2IhvWKuI1");
            }
        }

        public static void PrintCompanyName()
        {
            using (var session = new EconomicWebServiceSoapClient())
            {
                Console.WriteLine("Connecting");
                Connect(session);

                var companyHandle = session.Company_Get();
                var companyData = session.Company_GetData(companyHandle);

                Console.WriteLine(companyData.Name);

                var projectHandle = session.Project_FindByNumber(1844);
                var timeEntriesData = session.Project_GetTimeEntries(projectHandle);


                //Fetch employees
                var employeeHandles = session.Employee_GetAll();
                foreach (EmployeeHandle employee in employeeHandles)
                {
                    var employeeData = session.Employee_GetData(employee);
                    if (employeeData.EmployeeType == EmployeeType.TimeLogger)
                    {
                        Console.WriteLine(employeeData.Name + " CostPrice=" + employeeData.CostPrice + " SalesPrice=" + employeeData.SalesPrice);
                        var timeEntries = session.Employee_GetTimeEntries(employee);
                        foreach (TimeEntryHandle timeEntry in timeEntries)
                        {
                            var timeEntryData = session.TimeEntry_GetData(timeEntry);
                            Console.WriteLine(timeEntryData.NumberOfHours + " " +timeEntryData.SalesPrice + " " + timeEntryData.Text);
                        }
                    }
                  

                    
                }

                


                Console.WriteLine("Disconnecting");
                session.Disconnect();
                Console.WriteLine("Done");
                Console.ReadKey();

            }
        }
    }
}
