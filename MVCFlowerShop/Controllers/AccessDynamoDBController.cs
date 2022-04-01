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
                attributes["Amount"] = new AttributeValue {S = amount.ToString() };
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
           

            return RedirectToAction("addData", "AccessDynamoDb", new { msg = message });
        }

        public IActionResult SearchBasedOnPrice(string msg="")
        {
            ViewBag.msg = msg;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchBasedOnPrice(string operators, string Price)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            List<Document> documentList = new List<Document>(); //dynamodbV2document model - use for reading from DB
            List<KeyValuePair<string, string>> singlerecord = new List<KeyValuePair<string, string>>(); //use for frontend
            List<List<KeyValuePair<string, string>>> records = new List<List<KeyValuePair<string, string>>>();

            try
            {
                //step 1: create statement /process for full scan and filter(condition - optional) data in side the table
                ScanFilter scanprice = new ScanFilter();
                if (operators == "=")
                    scanprice.AddCondition("Amount", ScanOperator.Equal, Price);  
                else if (operators == ">")
                    scanprice.AddCondition("Amount", ScanOperator.GreaterThan, Price);
                else if (operators == "<")
                    scanprice.AddCondition("Amount", ScanOperator.LessThan, Price);
                else if (operators == ">=")
                    scanprice.AddCondition("Amount", ScanOperator.GreaterThanOrEqual, Price);
                else if (operators == "<=")
                    scanprice.AddCondition("Amount", ScanOperator.LessThanOrEqual, Price);

                //step 2: execute the commands
                Table customerTransactions = Table.LoadTable(dynamoDbClient, tableName);
                Search search = customerTransactions.Scan(scanprice);

                //step 3: loop to get everything one by one from the reading
                do
                {
                    documentList = await search.GetNextSetAsync();
                    if (documentList.Count == 0)
                    {
                        ViewBag.msg = "Product not found!";
                        return View();
                    }

                    foreach (var document in documentList) //read the single record keys and values 1 by 1
                    {
                        singlerecord = GetValues(document); //GetValues - our own method - extract the key value one by one ans store in list (without using object oriented idea)
                        singlerecord.Sort(Compare1); //Compare1 - also our own method - sort the key attriibute in an ascending order
                        records.Add(singlerecord);
                    }

                } while (!search.IsDone);
                ViewBag.msg = "Product found!";
            }
            catch (AmazonDynamoDBException ex)
            {
                ViewBag.msg = "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                ViewBag.msg = "Error: " + ex.Message;
            }
            //step 4: bring the record to display in the frontend
            return View(records);
        }

        public List<KeyValuePair<string, string>> GetValues(Document document)
        {
            var records = new List<KeyValuePair<string, string>>();

            foreach (var attribute in document.GetAttributeNames())
            {
                string stringValue = null;
                var value = document[attribute];

                if (value is DynamoDBBool)
                {
                    stringValue = value.AsBoolean().ToString();
                }else if (value is Primitive)
                {
                    stringValue = value.AsPrimitive().ToString();
                }else if (value is PrimitiveList)
                {
                    stringValue = string.Join(",", (from primitive
                                                    in value.AsPrimitiveList().Entries
                                                    select primitive.Value).ToArray());
                }
                records.Add(new KeyValuePair<string, string>(attribute, stringValue));
            }
            return records;
        }

        static int Compare1 (KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            return a.Key.CompareTo(b.Key);
        }

        public async Task<IActionResult> deleteData(string cid, string tid)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            try
            {
                var deleteItem = new DeleteItemRequest
                {
                    TableName = tableName,
                    Key =
                    new Dictionary<string, AttributeValue>()
                    {
                        {"CustomerID", new AttributeValue{S=cid} },
                        {"TransactionID", new AttributeValue{S=tid} }
                    },
                };
                await dynamoDbClient.DeleteItemAsync(deleteItem);
                message = "Customer of " + cid + " with transaction is " + tid + " is deleted.";
            }
            catch (Exception ex)
            {
                message = "Error :" + ex.ToString();
            }
            return RedirectToAction("Index", "AccessDynamoDb", new { msg = message });
        }

        public async Task<IActionResult> editData(string cid, string tid) //need a interface to show the every single existing values
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var dynamoDbClient = new AmazonDynamoDBClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            List<Document> documentList = new List<Document>(); //dynamodbV2document model - use for reading from DB
            List<KeyValuePair<string, string>> singlerecord = new List<KeyValuePair<string, string>>(); //use for frontend
            List<List<KeyValuePair<string, string>>> records = new List<List<KeyValuePair<string, string>>>();

            try
            {
                QueryFilter queryFilter = new QueryFilter("CustomerID", QueryOperator.Equal, cid);
                queryFilter.AddCondition("TransactionID", QueryOperator.Equal, tid);

                Table customerTransactions = Table.LoadTable(dynamoDbClient, tableName);
                Search search = customerTransactions.Query(queryFilter);

                documentList = await search.GetNextSetAsync();
                if (documentList.Count == 0)
                {
                    ViewBag.msg = "Product not found!";
                    return View();
                }

                foreach (var document in documentList) //read the single record keys and values 1 by 1
                {
                    singlerecord = GetValues(document); //GetValues - our own method - extract the key value one by one ans store in list (without using object oriented idea)
                    singlerecord.Sort(Compare1); //Compare1 - also our own method - sort the key attriibute in an ascending order
                    records.Add(singlerecord);
                }
                ViewBag.msg = "Product found!";
            }
            catch (AmazonDynamoDBException ex)
            {
                message = "Error: " + ex.Message;
            }
            catch (Exception ex)
            {
                message = "Error: " + ex.Message;
            }

            return View(records); //display the single record in the front end
        }
    }
}