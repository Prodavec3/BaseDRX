using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagementUI.Server
{
  partial class DocumentsToReturnFolderHandlers
  {

    public virtual IQueryable<Sungero.Docflow.IOfficialDocument> DocumentsToReturnDataQuery(IQueryable<Sungero.Docflow.IOfficialDocument> query)
    {
      var today = Calendar.UserToday;
      var documents = query.Where(l => l.IsReturnRequired == true || l.IsHeldByCounterParty == true);
      
      documents = documents.Where(d => !Docflow.ContractualDocumentBases.Is(d) && !Docflow.AccountingDocumentBases.Is(d));
      
      if (_filter == null)
        return documents;

      // Фильтр по статусу.
      if (_filter.Overdue)
      {
        documents = documents.Where(l => l.Tracking.Any(d => d.ReturnDate > d.ReturnDeadline ||
                                                        (!d.ReturnDate.HasValue && d.ReturnDeadline < today)));
      }
      
      // Фильтр по виду документа.
      if (_filter.DocumentKind != null)
      {
        documents = documents.Where(l => Equals(l.DocumentKind, _filter.DocumentKind));
      }
      
      // Фильтр по сотруднику.
      if (_filter.Employee != null)
      {
        documents = documents.Where(l => l.Tracking.Any(d => Equals(d.DeliveredTo, _filter.Employee)));
      }
      
      // Фильтр по подразделению.
      if (_filter.Department != null)
      {
        documents = documents.Where(l => l.Tracking.Any(d => Equals(d.DeliveredTo.Department, _filter.Department)));
      }

      // Фильтр по группе регистрации.
      if (_filter.RegistrationGroup != null)
      {
        documents = documents.Where(l => l.DocumentRegister != null &&
                                    Equals(l.DocumentRegister.RegistrationGroup, _filter.RegistrationGroup));
      }
      
      // Фильтр по делу.
      if (_filter.Filelist != null)
      {
        documents = documents.Where(l => Equals(l.CaseFile, _filter.Filelist));
      }
      
      // Исключить строки из таблиц Выдачи с Результатом возврата: "Возвращен".
      var returned = Docflow.OfficialDocumentTracking.ReturnResult.Returned;

      // Фильтр по сроку возврата: до конца дня.
      if (_filter.EndDay)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline < today.AddDays(1)));

      // Фильтр по сроку возврата: до конца недели.
      if (_filter.EndWeek)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= today.EndOfWeek()));

      // Фильтр по сроку возврата: до конца месяца.
      if (_filter.EndMonth)
        documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= today.EndOfMonth()));

      // Фильтр по сроку возврата: в период с, по.
      if (_filter.Manual)
      {
        var dateFrom = _filter.ReturnPeriodDataRangeFrom;
        var dateTo = _filter.ReturnPeriodDataRangeTo;
        
        if (dateFrom.HasValue && !dateTo.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline >= dateFrom.Value));
        
        if (dateTo.HasValue && !dateFrom.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) && p.ReturnDeadline <= dateTo.Value));
        
        if (dateFrom.HasValue && dateTo.HasValue)
          documents = documents.Where(l => l.Tracking.Any(p => !Equals(p.ReturnResult, returned) &&
                                                          (p.ReturnDeadline >= dateFrom.Value &&
                                                           p.ReturnDeadline <= dateTo.Value)));
      }
      
      return documents;
    }
    
    public virtual IQueryable<Sungero.Docflow.IDocumentKind> DocumentsToReturnDocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query)
    {
      return query.Where(d => d.Status == CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Contracts &&
                         d.DocumentType.DocumentTypeGuid != Docflow.PublicConstants.AccountingDocumentBase.IncomingInvoiceGuid &&
                         d.DocumentType.DocumentTypeGuid != Docflow.PublicConstants.AccountingDocumentBase.IncomingTaxInvoiceGuid &&
                         d.DocumentType.DocumentTypeGuid != Docflow.PublicConstants.AccountingDocumentBase.OutcomingTaxInvoiceGuid);
    }
  }

  partial class RecordManagementUIHandlers
  {
  }
}