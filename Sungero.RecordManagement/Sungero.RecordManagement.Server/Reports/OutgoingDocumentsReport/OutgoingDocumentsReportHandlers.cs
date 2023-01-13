using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement
{
  partial class OutgoingDocumentsReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.OutgoingDocumentsReport.SourceTableName, OutgoingDocumentsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      OutgoingDocumentsReport.ReportSessionId = reportSessionId;
      var documents = Enumerable.Empty<Docflow.IOfficialDocument>().AsQueryable();
      AccessRights.AllowRead(() => { documents = this.GetOutgoingDocuments(); });
      this.WriteToOutgoingDocumentsTable(documents, reportSessionId);
    }
    
    /// <summary>
    /// Получить исходящие документы для журнала.
    /// </summary>
    /// <returns>Исходящие документы.</returns>
    public virtual IQueryable<Sungero.Docflow.IOutgoingDocumentBase> GetOutgoingDocuments()
    {
      return Sungero.Docflow.OutgoingDocumentBases.GetAll()
        .Where(l => l.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Outgoing)
        .Where(l => l.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered)
        .Where(l => Equals(l.DocumentRegister, OutgoingDocumentsReport.DocumentRegister))
        .Where(l => l.RegistrationDate >= OutgoingDocumentsReport.BeginDate)
        .Where(l => l.RegistrationDate <= OutgoingDocumentsReport.EndDate);
    }
    
    /// <summary>
    /// Заполнить отчет данными об исходящих документах.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="reportSessionId">ИД сессии отчета.</param>
    public virtual void WriteToOutgoingDocumentsTable(IQueryable<Docflow.IOfficialDocument> documents,
                                                      string reportSessionId)
    {
      var outgoingDocuments = new List<Structures.OutgoingDocumentsReport.TableLine>();
      var lineNumber = 0;
      foreach (var document in documents.ToList().OrderBy(d => d.RegistrationDate).ThenBy(d => d.Index))
      {
        lineNumber++;
        Structures.OutgoingDocumentsReport.TableLine documentInfo;
        var hasRightsOnDocument = document.AccessRights.CanRead();
        if (hasRightsOnDocument)
        {
          var preparedByName = string.Empty;
          var preparedByDepartmentShortName = string.Empty;
          var preparedByDepartmentName = string.Empty;
          if (document.PreparedBy != null)
          {
            preparedByName = document.PreparedBy.Name;
            if (document.PreparedBy.Department != null)
            {
              preparedByDepartmentShortName = document.PreparedBy.Department.ShortName;
              preparedByDepartmentName = document.PreparedBy.Department.Name;
            }
          }         
          documentInfo = Structures.OutgoingDocumentsReport.TableLine.Create(reportSessionId,
                                                                             lineNumber,
                                                                             document.RegistrationDate,
                                                                             document.RegistrationNumber,
                                                                             Functions.Module.GetOutgoingDocumentReportAddressee(document.Id),
                                                                             document.Department != null ? document.Department.ShortName : string.Empty,
                                                                             preparedByName,
                                                                             preparedByDepartmentShortName,
                                                                             preparedByDepartmentName,
                                                                             document.Subject,
                                                                             document.Note,
                                                                             true);
        }
        else
          documentInfo = Structures.OutgoingDocumentsReport.TableLine.Create(reportSessionId,
                                                                             lineNumber,
                                                                             document.RegistrationDate,
                                                                             document.RegistrationNumber,
                                                                             string.Empty,
                                                                             string.Empty,
                                                                             string.Empty,
                                                                             string.Empty,
                                                                             string.Empty,
                                                                             Sungero.Docflow.Resources.HaveNotEnoughAccessRights,
                                                                             string.Empty,
                                                                             false);
        
        outgoingDocuments.Add(documentInfo);
      }
      
      var tableName = Constants.OutgoingDocumentsReport.SourceTableName;
      Docflow.PublicFunctions.Module.WriteStructuresToTable(tableName, outgoingDocuments);
    }

  }

}