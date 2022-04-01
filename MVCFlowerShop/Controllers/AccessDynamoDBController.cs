using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MVCFlowerShopLab3_2022.Controllers
{
    public class AccessDynamoDbController : Controller
    {
        private const string tableName = "mvcFlowerShopTable";

        private List<string> getAWSCredentialInfo() //using function to get security credential info from json
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();
            List<string> credentialInfo = new List<string>();
            credentialInfo.Add(configure["AWSCredential:accesskey"]);
            credentialInfo.Add(configure["AWSCredential:secretkey"]);
            credentialInfo.Add(configure["AWSCredential:SessionToken"]);
            return credentialInfo;
        }

        public IActionResult Index(string msg = "")
        {
            ViewBag.msg = msg;
            return View();
        }

        public async Task<IActionResult> CreateTable()
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            try //create schema info of the table
            {
                var tableRequest = new CreateTableRequest
                {
                    //table setting
                    TableName = tableName,
                    AttributeDefinitions = new List<AttributeDefinition>()
                    {
                        new AttributeDefinition //partition key
                        {
                            AttributeName = "CustomerID",
                            AttributeType = "S"
                        },
                        new AttributeDefinition //sort key
                        {
                            AttributeName = "TransactionID",
                            AttributeType = "S"
                        }
                    },
                    KeySchema = new List<KeySchemaElement>()
                    {
                        new KeySchemaElement //partition key
                        {
                            AttributeName = "CustomerID",
                            KeyType = "HASH"
                        },
                        new KeySchemaElement //sort key
                        {
                            AttributeName = "TransactionID",
                            KeyType = "RANGE"
                        }
                    },
                    ProvisionedThroughput = new ProvisionedThroughput
                    {
                        ReadCapacityUnits = 5,
                        WriteCapacityUnits = 10
                    }
                };

                await dynamoDbClient.CreateTableAsync(tableRequest);
                message = tableName + " is created now!";
            }
            catch (Exception ex)
            {
                message = "Table unable to create. Error as below: \\n" + ex.Message;
            }
            return RedirectToAction("Index", "AccessDynamoDb", new { msg = message });
        }

        public IActionResult addData(string msg = "")
        {
            ViewBag.msg = msg;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> addData(string customerID, string productName,
            int quantity, double amount, string paymentType, string paymentDate)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>();
            try
            {
                attributes["CustomerID"] = new AttributeValue { S = customerID };
                attributes["TransactionID"] = new AttributeValue { S = Guid.NewGuid().ToString() };
                attributes["ProductName"] = new AttributeValue { S = productName };
                attributes["Quantity"] = new AttributeValue { S = quantity.ToString() };
                attributes["Amount"] = new AttributeValue { S = amount.ToString() };
                attributes["PaymentType"] = new AttributeValue { S = paymentType };
                
                if (paymentType == "cash" || paymentType == "bank")
                {
                    attributes["PaymentStatus"] = new AttributeValue { BOOL = true };
                    if (!string.IsNullOrEmpty(paymentDate))
                    {
                        attributes["PaymentDate"] = new AttributeValue { S = paymentDate };
                    }
                }
                else
                {
                    attributes["PaymentStatus"] = new AttributeValue { BOOL = false };
                    if (!string.IsNullOrEmpty(paymentDate))
                    {
                        attributes["EstimatedPaymentDate"] = new AttributeValue { S = paymentDate };
                    }
                }

                PutItemRequest putRequest = new PutItemRequest
                {
                    TableName = tableName,
                    Item = attributes
                };

                await dynamoDbClient.PutItemAsync(putRequest);
                message = "Order of " + customerID + " is made now! Thank you for purchase!";
            }
            catch(Exception ex)
            {
                message = "Error: " + ex.Message;
            }
           

            return RedirectToAction("addData", "AccessDynamoDB", new { msg = message });
        }

        public IActionResult SearchBasedOnPrice(string msg="")
        {
            ViewBag.msg = msg;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchBasedOnPrice(string operators, string price)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            List<Document> documentList = new List<Document>();
            List<KeyValuePair<string, string>> singleRecord = new List<KeyValuePair<string, string>>();
            List<List<KeyValuePair<string, string>>> records = new List<List<KeyValuePair<string, string>>>();

            try
            {
                ScanFilter scanPrice = new ScanFilter();
                if (operators == "=")
                    scanPrice.AddCondition("amount", ScanOperator.Equal, Convert.ToDouble(price));
                else if (operators == "<")
                    scanPrice.AddCondition("amount", ScanOperator.LessThan, Convert.ToDouble(price));
                else if (operators == ">")
                    scanPrice.AddCondition("amount", ScanOperator.GreaterThan, Convert.ToDouble(price));
                else if (operators == "<=")
                    scanPrice.AddCondition("amount", ScanOperator.LessThanOrEqual, Convert.ToDouble(price));
                else if (operators == ">=")
                    scanPrice.AddCondition("amount", ScanOperator.GreaterThanOrEqual, Convert.ToDouble(price));
                Table customerTransactions = Table.LoadTable(dynamoDbClient, tableName);
                Search search = customerTransactions.Scan(scanPrice);
                do
                {
                    documentList = await search.GetNextSetAsync();
                    if (documentList.Count == 0)
                    {
                        ViewBag.msg = "Product not found!";
                        return View();
                    }

                    foreach (var document in documentList)
                    {
                        singleRecord = GetValues(document);
                        singleRecord.Sort(Compare1);
                        records.Add(singleRecord);
                    }
                } while (!search.IsDone);
                ViewBag.msg = "Product found!";
            }
            catch (Exception ex)
            {
                ViewBag.msg = "Error: " + ex.Message;
            }
            return View(records);
        }
    }
}
