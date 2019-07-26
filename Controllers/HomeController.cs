using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InsuranceClientPortal.Models;
using InsuranceClientPortal.Models.ViewModel;
using System.IO;
using Microsoft.Extensions.Configuration;
using InsuranceClientPortal.Helpers;
using Newtonsoft.Json;

namespace InsuranceClientPortal.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration config;

        public HomeController(IConfiguration configuration)
        {
            this.config = configuration;
        }
        public IActionResult Index()
        {

            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CustomerViewModel customer)
        {
            if (ModelState.IsValid)
            {
                var customerId = Guid.NewGuid().ToString();
                //save data
                var tempfile = Path.GetTempFileName();
                using (var fs = new FileStream(tempfile, FileMode.Create, FileAccess.Write))
                {
                    await customer.ImageUrl.CopyToAsync(fs);
                }
                var fileName = Path.GetFileName(customer.ImageUrl.FileName);
                var tempPath = Path.GetDirectoryName(tempfile);
                var imagePath = Path.Combine(tempPath, string.Concat(customerId, "_", fileName));
                System.IO.File.Move(tempfile, imagePath);

                StorageHelper storageHelper = new StorageHelper();
                storageHelper.StorageConnectionString = config.GetConnectionString("StorageConnection");
                var imageUrl = await storageHelper.UploadFileAsync(imagePath, "images");
                System.IO.File.Delete(imagePath);

                storageHelper.TableConnectionString= config.GetConnectionString("TableConnection");
                Customer customerEntity = new Customer(customerId, customer.InsuranceType);
                customerEntity.Amount = customer.Amount;
                customerEntity.Name = customer.Name;
                customerEntity.Email = customer.Email;
                customerEntity.Premium = customer.Premium;
                customerEntity.AppDate = customer.AppDate;
                customerEntity.EndDate = customer.EndDate;
                customerEntity.ImageUrl = imageUrl;

                Customer newCustomer = await storageHelper.SaveInsuranceDetailAsync(customerEntity, "customers");

                //send message
                string messageText = JsonConvert.SerializeObject(newCustomer);
                await storageHelper.SendMessageAsync(messageText, "insurance-queue");
            }
            else
            {
                //error
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
