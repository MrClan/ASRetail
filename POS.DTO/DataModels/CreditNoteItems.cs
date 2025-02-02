﻿using POS.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.DTO
{
    [Table("CREDIT_NOTE_ITEMS")]
    public partial class CreditNoteItems
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Guid? Credit_Note_Id { get; set; }
        public string Credit_Note_Number { get; set; }
        public string ItemId { get; set; }
        public string ItemCode { get; set; }
        public string Bar_Code { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal? Rate { get; set; }
        public decimal RateExcludeVat { get; set; } = 0;
        public decimal RateExcludeVatWithoutRoundoff { get; set; } = 0;
        public decimal? Quantity { get; set; }
        public decimal? Gross_Amount { get; set; }
        public decimal? Discount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal PromoDiscount { get; set; } = 0;
        public decimal MembershipDiscount { get; set; } = 0;
        public bool Is_Discountable { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Net_Amount { get; set; }
        public bool? Is_Vatable { get; set; } = false;
        public string Remarks { get; set; }

        public bool IsNavSync { get; set; }
        public int SyncErrorCount { get; set; } = 0;
        public DateTime? NavSyncDate { get; set; }

        [ForeignKey("Credit_Note_Id")]
        public CreditNote CreditNote { get; set; }
    }
}
