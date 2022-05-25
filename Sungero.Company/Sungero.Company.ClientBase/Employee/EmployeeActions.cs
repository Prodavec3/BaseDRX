using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using Sungero.Company.Employee;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Client
{
  partial class EmployeeActions
  {
    public virtual void ShowRespondingDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {      
     var documents = Shell.PublicFunctions.Module.Remote.GetRespondingEmployeeDocuments(_obj);
     documents.Show(_obj.Person.ShortName);
    }

    public virtual bool CanShowRespondingDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void ShowResponsibilitiesReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Reports.GetResponsibilitiesReport();
      report.Employee = _obj;
      report.Open();
    }

    public virtual bool CanShowResponsibilitiesReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void OpenCertificatesList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Remote.GetCertificatesOfEmployee(_obj).Show();
    }

    public virtual bool CanOpenCertificatesList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }
}