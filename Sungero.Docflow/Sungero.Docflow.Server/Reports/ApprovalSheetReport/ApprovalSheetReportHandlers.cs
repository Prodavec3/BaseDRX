using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow
{
  partial class ApprovalSheetReportServerHandlers
  {
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(Constants.ApprovalSheetReport.SourceTableName, ApprovalSheetReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      ApprovalSheetReport.ReportSessionId = Guid.NewGuid().ToString();
      Functions.ApprovalTask.UpdateApprovalSheetReportTable(ApprovalSheetReport.Document, ApprovalSheetReport.ReportSessionId);
      ApprovalSheetReport.HasRespEmployee = false;
      
      var document = ApprovalSheetReport.Document;
      if (document == null)
        return;
      
      // Наименование отчета.
      ApprovalSheetReport.DocumentName = Docflow.PublicFunctions.Module.FormatDocumentNameForReport(document, false);
      
      // НОР.
      var ourOrg = document.BusinessUnit;
      if (ourOrg != null)
        ApprovalSheetReport.OurOrgName = ourOrg.Name;
      
      // Дата отчета.
      ApprovalSheetReport.CurrentDate = Calendar.Now;
      
      // Ответственный.
      var responsibleEmployee = Functions.OfficialDocument.GetDocumentResponsibleEmployee(document);
      
      if (responsibleEmployee != null &&
          responsibleEmployee.IsSystem != true)
      {
        var respEmployee = string.Format("{0}: {1}",
                                         Reports.Resources.ApprovalSheetReport.ResponsibleEmployee,
                                         responsibleEmployee.Person.ShortName);
        
        if (responsibleEmployee.JobTitle != null)
          respEmployee = string.Format("{0} ({1})", respEmployee, responsibleEmployee.JobTitle.DisplayValue.Trim());
        
        ApprovalSheetReport.RespEmployee = respEmployee;
        
        ApprovalSheetReport.HasRespEmployee = true;
      }
      
      // Распечатал.
      if (Employees.Current == null)
      {
        ApprovalSheetReport.Clerk = Users.Current.Name;
      }
      else
      {
        var clerkPerson = Employees.Current.Person;
        ApprovalSheetReport.Clerk = clerkPerson.ShortName;
      }
    }
  }
}