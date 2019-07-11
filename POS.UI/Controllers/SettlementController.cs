﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core;
using POS.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace POS.UI.Controllers
{
    [RolewiseAuthorized]
    public class SettlementController : Controller
    {

        private readonly EntityCore _context;

        public SettlementController(EntityCore context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            //IEnumerable<Settlement> settlement = _context.Settlement.Include(x => x.Terminal).Include(x => x.User).Where(x => x.Status == "Closed");
            //settlement = (from x in settlement
            //              group x by
            //              new { x.Id, x.PaymentMode, x.Remarks, x.SessionId, x.Status, x.TerminalId, x.TransactionDate, x.TransactionNumber, x.UserId, x.VerifiedBy, x.VerifiedDate } into y
            //              select new Settlement
            //              {
            //                  Id = y.Key.Id,
            //                  UserId
            //              });
            // .GroupBy(x => new { x.UserId, x.TerminalId })
            //.Select(y => new Settlement{
            //    Id = x=>x.
            //    x.Sum(y => y.Amount));
            IEnumerable<SettlementViewModel> settlement = _context.SettlementViewModel.Where(x => x.Status == "Closed");
            return View(settlement);
        }


        [HttpGet]
        public IActionResult GetSettlement(string status = "Closed")
        {

            IEnumerable<SettlementViewModel> settlement = _context.SettlementViewModel.Where(x => x.Status == status).ToList();
            return Ok(settlement);
        }

        [HttpGet]
        public IActionResult GetSettlementById(string session)
        {

            IEnumerable<SettlementViewModel> settlementData = _context.SettlementViewModel.Where(x => x.SessionId == session).ToList();
            var store = _context.Store.FirstOrDefault();
            return Ok(new { SettlementData = settlementData, Store = store });
        }


        [HttpPost]
        public IActionResult VerifySettlement([FromBody] SettlementPostViewModel data)
        {
            if (ModelState.IsValid)
            {
                IList<Settlement> settlement = _context.Settlement.Where(x => x.SessionId == data.Settlement.SessionId).ToList();
                var store = _context.Store.FirstOrDefault();
                foreach (var item in settlement)
                {
                    if (item.PaymentMode == "Cash")
                    {
                        item.AdjustmentAmount = data.AdjustmentCashAmount;
                        item.ShortExcessAmount = data.ShortExcessCashAmount;
                    }
                    else if (item.PaymentMode == "Credit")
                    {
                        item.AdjustmentAmount = data.AdjustmentCreditAmount;
                        item.ShortExcessAmount = data.ShortExcessCreditAmount;
                    }
                    else if (item.PaymentMode == "CreditNote")
                    {
                        item.AdjustmentAmount = data.AdjustmentCreditNoteAmount;
                        item.ShortExcessAmount = data.ShortExcessCreditNoteAmount;
                    }
                    else if (item.PaymentMode == "Card")
                    {
                        item.AdjustmentAmount = data.AdjustmentCardAmount;
                        item.ShortExcessAmount = data.ShortExcessCardAmount;
                    }
                    item.VerifiedBy = User.Identity.Name;
                    item.VerifiedDate = DateTime.Now;
                    item.Status = "Verified";
                    _context.Entry(item).State = EntityState.Modified;
                }

                _context.SaveChanges();
                var settlementData = _context.SettlementViewModel.Where(x => x.SessionId == settlement.FirstOrDefault().SessionId).ToList();
                return Ok(new { SettlementData = settlementData, Store = store });
            }
            return StatusCode(400);
        }

    }
}
