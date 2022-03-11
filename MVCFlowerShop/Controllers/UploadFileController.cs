using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using System;

namespace MVCFlowerShop.Controllers
{
    public class UploadFileController : Controller
    {
        private const string bucketName = "mvcflowershopbuckettp055072";
        private IWebHostEnvironment _hostEnvironment;

        public UploadFileController(IWebHostEnvironment hostarea)
        {
            _hostEnvironment = hostarea;
        }
        
        private List<string> getAWSCredentialInfo()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configure = builder.Build();
            List<string> credentialInfo = new List<string>();
            credentialInfo.Add(configure["AWSCredential:AccessKey"]);
            credentialInfo.Add(configure["AWSCredential:SecretKey"]);
            credentialInfo.Add(configure["AWSCredential:SessionToken"]);

            return credentialInfo;
        }

        public IActionResult Index(string msg = "")
        {
            ViewBag.msg = msg;
            return View();
        }

        [HttpPost("FileUpload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(List<IFormFile> images)
        {
            long size = images.Sum(f => f.Length);
            var filePath = ""; string fileContents = null;
            int i = 1;
            foreach(var item in images)
            {
                if (item.ContentType.ToLower() != "text/plain")
                {
                    return BadRequest("The " + item.FileName + " unable to upload because uploaded file is not a text file.");
                }else if (item.Length == 0)
                {
                    return BadRequest("The " + item.FileName + " unable to upload because uploaded file is empty.");
                }else if (item.Length > 1048576)
                {
                    return BadRequest("The " + item.FileName + " unable to upload because uploaded file is exceed 1 MB.");
                }else
                {
                    //filePath = "C:\\Users\\User\\Downloads\\Testupload" + i + ".txt";
                    filePath = Path.Combine(_hostEnvironment.WebRootPath, "images", item.FileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await item.CopyToAsync(stream);
                    }
                    using (var reader = new StreamReader(item.OpenReadStream(), 
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), 
                        detectEncodingFromByteOrderMarks: true)) 
                    { 
                        fileContents = fileContents + await reader.ReadToEndAsync(); 
                    }
                }
                i++;
            }
            return RedirectToAction("Index", "UploadFile", new {msg = fileContents});
        }

        [HttpPost("UploadToS3")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> images)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1], 
                credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            foreach(var image in images)
            {
                var uploadRequest = new PutObjectRequest()
                {
                    InputStream = image.OpenReadStream(),
                    BucketName = bucketName + "/images",
                    Key= image.FileName,
                    CannedACL = S3CannedACL.PublicRead
                };
                PutObjectResponse result =  await S3Client.PutObjectAsync(uploadRequest);
            }
            message = "All the files are uploaded to S3";
            return RedirectToAction("Index", "UploadFile", new { msg = message });
        }

        public async Task<IActionResult> ViewImages()
        {
            List<string> credentialInfo = getAWSCredentialInfo();
            var displayResult = new List<S3Object>();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1]
                , credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            string token = null;
            List<string> presignedURLs = new List<string>();
            try
            {
                do
                {
                    ListObjectsRequest viewRequest = new ListObjectsRequest()
                    {
                        BucketName = bucketName
                    };
                    ListObjectsResponse response = await S3Client.ListObjectsAsync(viewRequest).ConfigureAwait(false);
                    displayResult.AddRange(response.S3Objects);
                    token = response.NextMarker;
                }
                while (token  != null);
                foreach (var item in displayResult)
                {
                    GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                    {
                        BucketName = item.BucketName,
                        Key = item.Key,
                        Expires = DateTime.Now.AddMinutes(2)
                    };
                    presignedURLs.Add(S3Client.GetPreSignedURL(request));
                }
                ViewBag.ImageLinks = presignedURLs;
            }
            catch(Exception ex)
            {

            }
            return View(displayResult);
        }

    }
}
