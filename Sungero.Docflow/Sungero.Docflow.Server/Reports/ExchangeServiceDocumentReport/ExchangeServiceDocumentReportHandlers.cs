using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class ExchangeServiceDocumentReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Functions.Module.DeleteReportData(ExchangeServiceDocumentReport.DocumentsTableName, ExchangeServiceDocumentReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var documentsTableName = Constants.ExchangeServiceDocumentReport.SourceTableName;
      ExchangeServiceDocumentReport.DocumentsTableName = documentsTableName;
      var reportSessionId = System.Guid.NewGuid().ToString();
      ExchangeServiceDocumentReport.ReportSessionId = reportSessionId;
      
      #region Параметры отчета
      
      var resources = Sungero.Docflow.Reports.Resources.ExchangeServiceDocumentReport;
      var parameterInfo = string.Empty;
      if (ExchangeServiceDocumentReport.BusinessUnit != null)
        parameterInfo += string.Format("{0}: {1}\n", Resources.BusinessUnit, ExchangeServiceDocumentReport.BusinessUnit.Name);
      
      if (ExchangeServiceDocumentReport.Department != null)
        parameterInfo += string.Format("{0}: {1}\n", Resources.Department, ExchangeServiceDocumentReport.Department.Name);
      
      if (ExchangeServiceDocumentReport.Employee != null)
        parameterInfo += string.Format("{0}: {1}\n", Resources.Employee,
                                       ExchangeServiceDocumentReport.Employee.Person.ShortName ?? ExchangeServiceDocumentReport.Employee.Name);
      
      if (ExchangeServiceDocumentReport.Counterparty != null)
        parameterInfo += string.Format("{0}: {1}\n", resources.Counterparty, ExchangeServiceDocumentReport.Counterparty.Name);
      
      parameterInfo += string.Format("{0}: {1}", resources.ExchangeService,
                                     string.Join(", ", ExchangeServiceDocumentReport.ExchangeService.Select(x => x.Name)));
      
      ExchangeServiceDocumentReport.ParametersInfo = parameterInfo;
      
      #endregion
      
      var sendFrom = ExchangeServiceDocumentReport.SendDateFrom;
      if (sendFrom.HasValue)
        ExchangeServiceDocumentReport.SendPeriod += string.Format("{0} {1}",
                                                                  Sungero.Docflow.Reports.Resources.ExchangeServiceDocumentReport.PeriodFrom,
                                                                  sendFrom.Value.ToString("d"));
      var sendTo = ExchangeServiceDocumentReport.SendDateTo;
      if (sendTo.HasValue)
        ExchangeServiceDocumentReport.SendPeriod += string.Format(" {0} {1}",
                                                                  Sungero.Docflow.Reports.Resources.ExchangeServiceDocumentReport.PeriodTo,
                                                                  sendTo.Value.ToString("d"));
      
      // Отфильтровать доступные документы.
      var documents = Docflow.OfficialDocuments.GetAll()
        .Where(d => d.ExchangeState == Docflow.OfficialDocument.ExchangeState.SignAwaited)
        .Where(d => d.Tracking.Any(t => t.ExternalLinkId.HasValue && !t.ReturnDate.HasValue))
        .Where(d => d.Versions.Any());
      
      // Фильтр по дате отправки.
      if (sendFrom.HasValue)
        documents = documents.Where(d => d.Tracking.Any(t => t.ExternalLinkId.HasValue && t.DeliveryDate >= sendFrom && !t.ReturnDate.HasValue));
      if (sendTo.HasValue)
        documents = documents.Where(d => d.Tracking.Any(t => t.ExternalLinkId.HasValue && t.DeliveryDate <= sendTo && !t.ReturnDate.HasValue));
      
      // Фильтр по НОР.
      if (ExchangeServiceDocumentReport.BusinessUnit != null)
        documents = documents.Where(d => d.BusinessUnit == null || Equals(d.BusinessUnit, ExchangeServiceDocumentReport.BusinessUnit));
      
      // Фильтр по подразделению сотрудника.
      if (ExchangeServiceDocumentReport.Department != null)
        documents = documents.Where(d => d.Tracking.Any(t => t.ExternalLinkId.HasValue && !t.ReturnDate.HasValue &&
                                                        Equals(t.DeliveredTo.Department, ExchangeServiceDocumentReport.Department)));
      
      // Фильтр по сотруднику.
      if (ExchangeServiceDocumentReport.Employee != null)
        documents = documents.Where(d => d.Tracking.Any(t => t.ExternalLinkId.HasValue && !t.ReturnDate.HasValue &&
                                                        Equals(t.DeliveredTo, ExchangeServiceDocumentReport.Employee)));
      
      // Инфошки.
      var exchangeDocumentsInfos = Exchange.ExchangeDocumentInfos.GetAll()
        .Where(x => documents.Contains(x.Document))
        .Where(x => x.MessageType == Exchange.ExchangeDocumentInfo.MessageType.Outgoing);
      
      // Заполнить данные для временной таблицы.
      var dataTable = new List<Structures.ExchangeServiceDocumentReport.ExchangeServiceDocumentTableLine>();
      foreach (var document in documents)
      {
        var tableLine = Structures.ExchangeServiceDocumentReport.ExchangeServiceDocumentTableLine.Create();
        
        tableLine.ReportSessionId = reportSessionId;

        // Инфо об отправки документа.
        var documentExchangeInfo = exchangeDocumentsInfos.Where(x => Equals(x.Document, document)).OrderByDescending(x => x.MessageDate).FirstOrDefault();
        if (documentExchangeInfo == null)
          continue;
        
        // НОР.
        var businessUnit = document.BusinessUnit;
        if (businessUnit == null)
          businessUnit = ExchangeCore.PublicFunctions.BoxBase.GetBusinessUnit(documentExchangeInfo.Box);
        
        // Дофильтрация по НОР.
        if (ExchangeServiceDocumentReport.BusinessUnit != null && !Equals(businessUnit, ExchangeServiceDocumentReport.BusinessUnit))
          continue;
        
        tableLine.BusinessUnitName = businessUnit != null ? businessUnit.Name : string.Empty;
        tableLine.BusinessUnitId = businessUnit != null ? businessUnit.Id : 0;
        
        // Получатель.
        var counterparty = documentExchangeInfo.Counterparty;
        tableLine.Counterparty = counterparty != null ? counterparty.Name : string.Empty;
        
        // Фильтр по контрагенту.
        if (ExchangeServiceDocumentReport.Counterparty != null && counterparty != null &&
            !Equals(counterparty, ExchangeServiceDocumentReport.Counterparty))
          continue;
        
        // Сервис обмена.
        var exchangeService = ExchangeCore.PublicFunctions.BoxBase.GetExchangeService(documentExchangeInfo.Box);
        tableLine.ExchangeService = exchangeService.Name;

        // Фильтр по сервисам обмена.
        var exchangeServices = ExchangeServiceDocumentReport.ExchangeService.ToList();
        if (!exchangeServices.Contains(exchangeService))
          continue;
        
        // Ответственный и его подразделение.
        var tracking = document.Tracking
          .Where(t => t.ExternalLinkId.HasValue && !t.ReturnDate.HasValue)
          .OrderByDescending(t => t.DeliveryDate)
          .FirstOrDefault();
        if (tracking != null)
        {
          var employee = tracking.DeliveredTo;
          tableLine.Responsible = employee.Person.ShortName ?? employee.Name;
          tableLine.Department = employee.Department != null ? string.Format("({0})", employee.Department.Name) : string.Empty;
        }
        else
        {
          tableLine.Responsible = string.Empty;
          tableLine.Department = string.Empty;
        }
        
        // Дата отправки.
        tableLine.SendDate = documentExchangeInfo.MessageDate.HasValue ?
          documentExchangeInfo.MessageDate.Value.ToUserTime().ToString("d") :
          "-";
        
        // Время ожидания контрагента.
        tableLine.Delay = WorkingTime.GetDurationInWorkingDays(documentExchangeInfo.MessageDate.Value, Calendar.Now).ToString();
        
        // Документ.
        tableLine.DocName = document.Name;
        tableLine.DocId = document.Id;
        tableLine.Hyperlink = Hyperlinks.Get(document);
        
        dataTable.Add(tableLine);
      }

      Docflow.PublicFunctions.Module.WriteStructuresToTable(documentsTableName, dataTable);
    }
  }
}