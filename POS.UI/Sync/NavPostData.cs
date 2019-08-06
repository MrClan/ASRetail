﻿using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using POS.Core;
using POS.DTO;
using POS.UI.Helper;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace POS.UI.Sync
{
    public class NavPostData
    {

        private readonly EntityCore _context;
        private readonly IMapper _mapper;
        public NavPostData(EntityCore context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public bool PostSalesInvoice(NavSalesInvoice invoice)
        {
            Config config = ConfigJSON.Read();
            if (config.StopInvoicePosting == false)
            {
                NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "SalesInvoicePost");
                string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";
                var client = NAV.NAVClient(url, config);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");


                request.RequestFormat = DataFormat.Json;
                var temp = JsonConvert.SerializeObject(invoice);
                request.AddJsonBody(temp);

                IRestResponse<SyncModel<NavSalesInvoice>> response = client.Execute<SyncModel<NavSalesInvoice>>(request);


                 if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
                {
                    //update sync status
                    SalesInvoice sInvoice = _context.SalesInvoice.FirstOrDefault(x => x.Invoice_Number == invoice.number);
                    sInvoice.IsNavSync = true;
                    sInvoice.NavSyncDate = DateTime.Now;
                    _context.Entry(sInvoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;


                    _context.SaveChanges();
                    PostSalesInvoicePaymentMode(invoice.number);
                    PostSalesInvoiceItem(invoice.number, sInvoice.Trans_Type);

                    return true;
                }
                else
                {
                    //update values
                    SalesInvoice sInvoice = _context.SalesInvoice.FirstOrDefault(x => x.Invoice_Number == invoice.number);
                    if (sInvoice.SyncErrorCount < 3)
                    {
                        sInvoice.SyncErrorCount = sInvoice.SyncErrorCount + 1;
                        _context.Entry(sInvoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        _context.SaveChanges();
                        //run scheduler after 1 minute
                       // BackgroundJob.Schedule(() => PostSalesInvoice(invoice), TimeSpan.FromMinutes(sInvoice.SyncErrorCount * 5));
                    }

                    return false;
                }
            }
            else
                return true;
        }
        public bool PostSalesInvoice(Store store)
        {
            Config config = ConfigJSON.Read();
            
            var unSyncInvoice = _context.SalesInvoice.Where(x => x.IsNavSync == false).OrderBy(x=>x.Trans_Date_Ad);
            int errorCount = 0,successCount = 0;
           foreach(var salesInvoice in unSyncInvoice)
            {
                NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "SalesInvoicePost");
                string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";
                var client = NAV.NAVClient(url, config);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");

                NavSalesInvoice invoice = new NavSalesInvoice()
                {
                    id = salesInvoice.Id.ToString(),
                    number = salesInvoice.Invoice_Number,
                    postingno = salesInvoice.Invoice_Number,
                    shippingno = salesInvoice.Invoice_Number,
                    orderDate = salesInvoice.Trans_Date_Ad.Value.ToString("yyyy-MM-dd"),
                    customerNumber = salesInvoice.MemberId,
                    customerName = salesInvoice.Customer_Name,
                    vatregistrationnumber = salesInvoice.Customer_Vat,
                    locationcode = store.INITIAL,
                    accountabilitycenter = store.INITIAL,
                    assigneduserid = salesInvoice.Created_By,
                    amountrounded = salesInvoice.Total_Net_Amount != salesInvoice.Total_Payable_Amount
                    

                };

                request.RequestFormat = DataFormat.Json;
                var temp = JsonConvert.SerializeObject(invoice);
                request.AddJsonBody(temp);

                IRestResponse<SyncModel<NavSalesInvoice>> response = client.Execute<SyncModel<NavSalesInvoice>>(request);


                if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
                {
                    //update sync status
                    SalesInvoice sInvoice = _context.SalesInvoice.FirstOrDefault(x => x.Invoice_Number == invoice.number);
                    sInvoice.IsNavSync = true;
                    sInvoice.NavSyncDate = DateTime.Now;
                    _context.Entry(sInvoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;


                    _context.SaveChanges();
                    PostSalesInvoicePaymentMode(invoice.number);
                    PostSalesInvoiceItem(invoice.number, sInvoice.Trans_Type);

                    successCount += 1;
                    config.Environment = "Success " + successCount + " - Error " + errorCount;
                    ConfigJSON.Write(config);
                   // return true;
                }
                else
                {
                    ////update values
                    //SalesInvoice sInvoice = _context.SalesInvoice.FirstOrDefault(x => x.Invoice_Number == invoice.number);
                    //if (sInvoice.SyncErrorCount < 3)
                    //{
                    //    sInvoice.SyncErrorCount = sInvoice.SyncErrorCount + 1;
                    //    _context.Entry(sInvoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    //    _context.SaveChanges();
                    //    //run scheduler after 1 minute
                    //    BackgroundJob.Schedule(() => PostSalesInvoice(invoice), TimeSpan.FromMinutes(sInvoice.SyncErrorCount * 5));
                    //}
                    errorCount += 1;
                    config.Environment = "Success " + successCount + " - Error " + errorCount;
                    ConfigJSON.Write(config);
                   // return false;
                }
            }
            config.Environment += " Finished";
            ConfigJSON.Write(config);
            return true;
        }
        public bool PostTaxInvoice(SalesInvoice invoice)
        {
            Config config = ConfigJSON.Read();
            if (config.StopInvoicePosting)
                return true;

            NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "TaxInvoicePost");
            string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";
            var client = NAV.NAVClient(url, config);
            var request = new RestRequest(Method.POST);

            request.AddHeader("Content-Type", "application/json");


            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(invoice);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
                return true;
            else
            {
                return false;
            }
        }
        public bool PostCreditNote(NavCreditMemo creditNote)
        {

            Config config = ConfigJSON.Read();
            if (config.StopCreditNotePosting)
                return true;
            NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "CreditNotePost");
            string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";
            var client = NAV.NAVClient(url, config);
            var request = new RestRequest(Method.POST);

            request.AddHeader("Content-Type", "application/json");


            request.RequestFormat = DataFormat.Json;
            var temp = JsonConvert.SerializeObject(creditNote);
            request.AddJsonBody(temp);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                //update sync status
                CreditNote sInvoice = _context.CreditNote.FirstOrDefault(x => x.Credit_Note_Number == creditNote.number);
                sInvoice.IsNavSync = true;
                sInvoice.NavSyncDate = DateTime.Now;
                _context.Entry(sInvoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;


                _context.SaveChanges();

                PostCreditNoteItem(sInvoice.Credit_Note_Number);
                return true;
            }
            else
            {
                //update values
                //CreditNote sInvoice = _context.CreditNote.FirstOrDefault(x => x.Credit_Note_Number == creditNote.number);
                //if (sInvoice.SyncErrorCount < 3)
                //{
                //    sInvoice.SyncErrorCount = sInvoice.SyncErrorCount + 1;
                //    _context.Entry(sInvoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                //    _context.SaveChanges();
                //    //run scheduler after 1 minute
                //    BackgroundJob.Schedule(() => PostSalesInvoice(invoice), TimeSpan.FromMinutes(sInvoice.SyncErrorCount * 5));
                //}
                return false;
            }
        }
        public void PostCustomer()
        {
            Config config = ConfigJSON.Read();
            if (config.StopCustomerPosting)
            {
                NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "Customer");
                string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";
                var client = NAV.NAVClient(url, config);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");

                List<Customer> customers = _context.Customer.Where(x => x.Is_Member == true && x.IsNavSync == false).ToList();
                var navCustomers = _mapper.Map<List<NavCustomerPOST>>(customers);
                request.RequestFormat = DataFormat.Json;

                foreach (var cus in navCustomers)
                {
                    var settings = new JsonSerializerSettings();
                    //settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    //you can add multiple settings and then use it
                    var obj = JsonConvert.SerializeObject(cus, Formatting.Indented, settings);

                    request.AddJsonBody(obj);
                    IRestResponse<SyncModel<NavCustomer>> response = client.Execute<SyncModel<NavCustomer>>(request);
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        var updatedCustomer = customers.FirstOrDefault(x => x.Membership_Number == cus.number);
                        updatedCustomer.IsNavSync = true;
                        updatedCustomer.NavSyncDate = DateTime.Now;


                        ////update update number
                        //var data = JsonConvert.DeserializeObject<NavCustomer>(response.Content);
                        //if (data != null)
                        //{
                        //    services.LastUpdateNumber = data.Update_No;
                        //    services.LastSyncDate = DateTime.Now;
                        //}
                        _context.SaveChanges();
                    }
                    else
                    {
                        if (response.Content.Contains("already exist"))
                        {


                            // update update number
                            var updatedCustomer = customers.FirstOrDefault(x => x.Membership_Number == cus.number);
                            updatedCustomer.IsNavSync = true;
                            updatedCustomer.NavSyncDate = DateTime.Now;

                            _context.SaveChanges();
                            continue;
                        }

                    }
                }
                if (_context.Customer.Where(x => x.Is_Member == true && x.IsNavSync == false).Any())
                {
                    BackgroundJob.Schedule(() => PostCustomer(), TimeSpan.FromMinutes(10));
                }

                //  request.AddJsonBody(navCustomers);

                ////IRestResponse response = client.Execute(request);
                //IRestResponse<SyncModel<NavCustomer>> response = client.Execute<SyncModel<NavCustomer>>(request);

                //if (response.StatusCode == HttpStatusCode.OK)
                //{
                //    List<Customer> navCustomer = _mapper.Map<List<Customer>>(response.Data.value);
                //    List<int?> memberIds = navCustomer.Select(x => x.Member_Id).ToList();
                //    var updatedCustomer = customers.Where(x => memberIds.Contains(x.Member_Id)).ToList();
                //    updatedCustomer.ForEach(x => { x.IsNavSync = true; x.NavSyncDate = DateTime.Now; });

                //    _context.SaveChanges();
                //    return true;
                //}
                //else
                //    return false;
            }
        }
        public bool PostSalesInvoicePaymentMode(string invoiceNumber)
        {
            Config config = ConfigJSON.Read();
            NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "SalesInvoicePost");
            NavIntegrationService serviceforSalesInvoiceItem = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "SalesPaymentModes");
            string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";

            var invoiceBill = _context.SalesInvoiceBill.Where(x => x.Invoice_Number == invoiceNumber);
            //var itemList = _context.SalesInvoiceItems.Where(x=> all)
            int lineNo = 0;
            foreach (var i in invoiceBill)
            {
                //if credit then no need to call api
                if (i.Trans_Mode == "Credit")
                    return false;
                lineNo += 1;
                NavSalesPaymentMode mode = new NavSalesPaymentMode()
                {
                    lineno = lineNo * 10000,
                    amount = i.Amount,
                    paymenttype = i.Trans_Mode == "CreditNote" ? "" : i.Trans_Mode, //for creditNote payment type should be blank
                    locationcode = config.Location,
                    documentno = i.Invoice_Number
                };
                var newUrl = url + $"({i.Invoice_Id.ToString()})/{serviceforSalesInvoiceItem.ServiceName}";

                var client = NAV.NAVClient(newUrl, config);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");


                request.RequestFormat = DataFormat.Json;
                var temp = JsonConvert.SerializeObject(mode);
                request.AddJsonBody(JsonConvert.SerializeObject(mode));

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    //update sync status
                    i.IsNavSync = true;
                    i.NavSyncDate = DateTime.Now;
                    _context.Entry(i).State = Microsoft.EntityFrameworkCore.EntityState.Modified;


                }

            }
            _context.SaveChanges();


            return true;


        }
        public bool PostSalesInvoiceItem(string invoiceNumber, string transType)
        {
            Config config = ConfigJSON.Read();
            NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "SalesInvoicePost");
            NavIntegrationService serviceforSalesInvoiceItem = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "SalesInvoicePostItem");
            string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";


            var items = _context.SalesInvoiceItems.Where(x => x.Invoice_Number == invoiceNumber);
            foreach (var item in items)
            {
               
                NavSalesItems navSalesItem = new NavSalesItems()
                {

                    itemId = item.ItemId,
                    itemno = item.ItemCode,
                    quantity = item.Quantity.Value,
                    unitPrice = item.RateExcludeVatWithoutRoundoff,// + item.RateExcludeVat * 13 / 100, //Send rate excluding vat
                    discountPercent = item.DiscountPercent


                };


                var newUrl = url + $"({items.FirstOrDefault().Invoice_Id.ToString()})/{serviceforSalesInvoiceItem.ServiceName}";

                var client = NAV.NAVClient(newUrl, config);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");


                request.RequestFormat = DataFormat.Json;
                var temp = JsonConvert.SerializeObject(navSalesItem);
                request.AddJsonBody(JsonConvert.SerializeObject(navSalesItem));

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    //update sync status
                    item.IsNavSync = true;
                    item.NavSyncDate = DateTime.Now;
                    _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _context.SaveChanges();

                }


            }

            if (items.Where(x => x.IsNavSync == false).Count() == 0)
            {
                PostSalesInvoiceCompletedSignalToNav(config,items.FirstOrDefault().Invoice_Id.Value);
            }
            //var allUnSyncItemList = _context.SalesInvoiceItems.Where(x => x.IsNavSync == false).Select(x => x.Invoice_Number).Distinct();
            ////var itemList = _context.SalesInvoiceItems.Where(x=> all)
            //foreach (var itemList in allUnSyncItemList)
            //{
            //    var items = _context.SalesInvoiceItems.Where(x => x.Invoice_Number == invoiceNumber);
            //    foreach (var item in items)
            //    {
            //        NavSalesItems navSalesItem = new NavSalesItems()
            //        {

            //            itemId = item.ItemId,
            //            unitPrice = item.Rate.Value,
            //            quantity = item.Quantity.Value,
            //            discountAmount = item.Discount.Value,

            //        };


            //        var newUrl = url + $"({items.FirstOrDefault().Invoice_Id.ToString()})/{serviceforSalesInvoiceItem.ServiceName}";

            //        var client = NAV.NAVClient(newUrl, config);
            //        var request = new RestRequest(Method.POST);

            //        request.AddHeader("Content-Type", "application/json");


            //        request.RequestFormat = DataFormat.Json;
            //        var temp = JsonConvert.SerializeObject(navSalesItem);
            //        request.AddJsonBody(JsonConvert.SerializeObject(navSalesItem));

            //        IRestResponse response = client.Execute(request);

            //        if (response.StatusCode == HttpStatusCode.Created)
            //        {
            //            //update sync status
            //            item.IsNavSync = true;
            //            item.NavSyncDate = DateTime.Now;
            //            _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            //            _context.SaveChanges();

            //        }


            //    }

            //    if (items.Where(x => x.IsNavSync == false).Count() == 0)
            //    {
            //        PostSalesInvoiceCompletedSignalToNav(items.FirstOrDefault());
            //    }
            //}


            return true;


        }
        public bool PostCreditNoteItem()
        {
            Config config = ConfigJSON.Read();
            NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "CreditNotePost");
            NavIntegrationService serviceforSalesInvoiceItem = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "CreditNotePostItem");
            string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";

            var allUnSyncItemList = _context.CreditNoteItem.Where(x => x.IsNavSync == false).Select(x => x.Credit_Note_Number).Distinct();
            //var itemList = _context.SalesInvoiceItems.Where(x=> all)
            foreach (var itemList in allUnSyncItemList)
            {
                var items = _context.CreditNoteItem.Where(x => x.Credit_Note_Number == itemList);
                foreach (var item in items)
                {
                    NavCreditItems navSalesItem = new NavCreditItems()
                    {

                        // itemId = item.it,
                        itemId = item.ItemId,
                        //itemno = item.ItemCode,
                        unitPrice = item.RateExcludeVat,
                        quantity = item.Quantity.Value,
                        discountPercent = item.DiscountPercent
                    };


                    var newUrl = url + $"({items.FirstOrDefault().Credit_Note_Id.ToString()})/{serviceforSalesInvoiceItem.ServiceName}";

                    var client = NAV.NAVClient(newUrl, config);
                    var request = new RestRequest(Method.POST);

                    request.AddHeader("Content-Type", "application/json");


                    request.RequestFormat = DataFormat.Json;
                    var temp = JsonConvert.SerializeObject(navSalesItem);
                    request.AddJsonBody(JsonConvert.SerializeObject(navSalesItem));

                    IRestResponse response = client.Execute(request);

                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        //update sync status
                        item.IsNavSync = true;
                        item.NavSyncDate = DateTime.Now;
                        _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                        _context.SaveChanges();

                    }


                }
                PostCreditInvoiceCompletedSignalToNav(config,items.FirstOrDefault().Credit_Note_Id.Value);


            }


            return true;


        }
        public bool PostCreditNoteItem(string invoiceNumber)
        {
            Config config = ConfigJSON.Read();
            NavIntegrationService services = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "CreditNotePost");
            NavIntegrationService serviceforSalesInvoiceItem = _context.NavIntegrationService.FirstOrDefault(x => x.IntegrationType == "CreditNotePostItem");
            string url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/{services.ServiceName}";

            
            var items = _context.CreditNoteItem.Where(x => x.Credit_Note_Number == invoiceNumber);
            foreach (var item in items)
            {
                NavCreditItems navSalesItem = new NavCreditItems()
                {

                    // itemId = item.it,
                    itemId = item.ItemId,
                    //itemno = item.ItemCode,
                    unitPrice = item.RateExcludeVat,
                    quantity = item.Quantity.Value,
                    discountPercent = item.DiscountPercent
                };


                var newUrl = url + $"({items.FirstOrDefault().Credit_Note_Id.ToString()})/{serviceforSalesInvoiceItem.ServiceName}";

                var client = NAV.NAVClient(newUrl, config);
                var request = new RestRequest(Method.POST);

                request.AddHeader("Content-Type", "application/json");


                request.RequestFormat = DataFormat.Json;
                var temp = JsonConvert.SerializeObject(navSalesItem);
                request.AddJsonBody(JsonConvert.SerializeObject(navSalesItem));

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    //update sync status
                    item.IsNavSync = true;
                    item.NavSyncDate = DateTime.Now;
                    _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _context.SaveChanges();

                }


            }
            if (items.Where(x => x.IsNavSync == false).Count() == 0)
            {
                PostCreditInvoiceCompletedSignalToNav(config,items.FirstOrDefault().Credit_Note_Id.Value);
            }
           



            return true;


        }



        //********* Final POST
        public void PostSalesInvoiceCompletedSignalToNav(Config config, Guid invoiceId)
        {           
            var url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/salesOrders";
            var newUrl = url + $"({invoiceId.ToString()})/Microsoft.NAV.Post";
            var client = NAV.NAVClient(newUrl, config);
            var request = new RestRequest(Method.POST);

            request.AddHeader("Content-Type", "application/json");

            IRestResponse response = client.Execute(request);
            if(response.Content == "")
            {
                //posted successfully                
                SalesInvoice _invoice = _context.SalesInvoice.FirstOrDefault(x => x.Id == invoiceId);
                if (_invoice != null)
                {
                    _invoice.IsNavPosted = true;
                    _context.Entry(_invoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _context.SaveChanges();
                }
            }




        }
        public void PostCreditInvoiceCompletedSignalToNav(Config config, Guid invoiceId)
        {           
            var url = config.NavApiBaseUrl + "/" + config.NavPath + $"/companies({config.NavCompanyId})/salesCreditMemos";
            var newUrl = url + $"({invoiceId.ToString()})/Microsoft.NAV.Cancel";
            var client = NAV.NAVClient(newUrl, config);
            var request = new RestRequest(Method.POST);

            request.AddHeader("Content-Type", "application/json");

            IRestResponse response = client.Execute(request);
            if (response.Content == "")
            {
                //posted successfully                
                CreditNote _invoice = _context.CreditNote.FirstOrDefault(x => x.Id == invoiceId);
                if (_invoice != null)
                {
                    _invoice.IsNavPosted = true;
                    _context.Entry(_invoice).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    _context.SaveChanges();
                }
            }


        }
    }
}
