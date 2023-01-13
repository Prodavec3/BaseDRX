using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.RecordManagement
{
  partial class InternalDocumentsReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.InternalDocumentsReport.SourceTableName, InternalDocumentsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      InternalDocumentsReport.ReportSessionId = reportSessionId;
      var documents = Enumerable.Empty<Docflow.IOfficialDocument>().AsQueryable();
      AccessRights.AllowRead(() => { documents = this.GetInternalDocuments(); });

      this.WriteToInternalDocumentsTable(documents, reportSessionId);
      
    }
    
    /// <summary>
    /// Получить внутренние документы для журнала.
    /// </summary>
    /// <returns>Внутренние документы.</returns>
    public virtual IQueryable<Sungero.Docflow.IInternalDocumentBase> GetInternalDocuments()
    {
      return Sungero.Docflow.InternalDocumentBases.GetAll()
        .Where(d => d.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Inner)
        .Where(d => d.RegistrationState == RegistrationState.Registered)
        .Where(d => d.DocumentRegister != null && Equals(d.DocumentRegister.Id, InternalDocumentsReport.DocumentRegister.Id))
        .Where(d => d.RegistrationDate >= InternalDocumentsReport.BeginDate)
        .Where(d => d.RegistrationDate <= InternalDocumentsReport.EndDate);
    }
    
    /// <summary>
    /// Заполнить отчет данными о внутренних документах.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="reportSessionId">ИД сессии отчета.</param>
    public virtual void WriteToInternalDocumentsTable(IQueryable<Docflow.IOfficialDocument> documents,
                                                      string reportSessionId)
    {
      var internalDocuments = new List<Structures.InternalDocumentsReport.TableLine>();
      var lineNumber = 0;
      foreach (var document in documents.ToList().OrderBy(d => d.RegistrationDate).ThenBy(d => d.Index))
      {
        lineNumber++;
        Structures.InternalDocumentsReport.TableLine documentInfo;
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
          documentInfo = Structures.InternalDocumentsReport.TableLine.Create(reportSessionId,
                                                                             lineNumber,
                                                                             document.RegistrationDate,
                                                                             document.RegistrationNumber,
                                                                             preparedByName,
                                                                             preparedByDepartmentShortName,
                                                                             preparedByDepartmentName,
                                                                             document.Subject,
                                                                             true);
        }
        else
          documentInfo = Structures.InternalDocumentsReport.TableLine.Create(reportSessionId,
                                                                             lineNumber,
                                                                             document.RegistrationDate,
                                                                             document.RegistrationNumber,
                                                                             string.Empty,
                                                                             string.Empty,
                                                                             string.Empty,
                                                                             Sungero.Docflow.Resources.HaveNotEnoughAccessRights,
                                                                             false);
        internalDocuments.Add(documentInfo);
      }
      
      var tableName = Constants.InternalDocumentsReport.SourceTableName;
      Docflow.PublicFunctions.Module.WriteStructuresToTable(tableName, internalDocuments);
    }
  }
}