﻿using System;
using System.Collections.Generic;
using System.Text;

namespace POS.DTO
{
    public class BillViewModel
    {
        public string username { get; set; }
        public string password { get; set; }
        public string seller_pan { get; set; }
        public string buyer_pan { get; set; }    //9 digit or empty        
        public string fiscal_year { get; set; }
        public string buyer_name { get; set; }
        public string invoice_number { get; set; }
        public string ref_invoice_number { get; set; }
        public string invoice_date { get; set; }
       public string  credit_note_date { get; set; }
        public string credit_note_number { get; set; }
        public string reason_for_return { get; set; }
        public double total_sales { get; set; }
        public double taxable_sales_vat { get; set; }
        public double vat { get; set; }
        public double excisable_amount { get; set; }
        public double excise { get; set; }
        public double taxable_sales_hst { get; set; }
        public double hst { get; set; }         //health service tax 
        public double amount_for_esf { get; set; }
        public double esf { get; set; }      //education service fee 
        public double export_sales { get; set; }
        public double tax_exempted_sales { get; set; }
        public bool isrealtime { get; set; }
        public DateTime datetimeClient { get; set; }
    }
}
