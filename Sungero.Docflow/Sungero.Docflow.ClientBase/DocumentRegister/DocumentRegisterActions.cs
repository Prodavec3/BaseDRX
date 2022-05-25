using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;

namespace Sungero.Docflow.Client
{
  partial class DocumentRegisterActions
  {
    public virtual void ShowDocumentKinds(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var documentKinds = Functions.DocumentKind.Remote.GetAllDocumentKinds();
      documentKinds.Show();
    }

    public virtual bool CanShowDocumentKinds(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowBusinessUnits(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var businessUnits = Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnits();
      businessUnits.Show();
    }

    public virtual bool CanShowBusinessUnits(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowDepartments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var departments = Company.PublicFunctions.Department.Remote.GetDepartments();
      departments.Show();
    }

    public virtual bool CanShowDepartments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowRegisteredDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.DocumentRegister.Remote.GetRegisteredDocuments(_obj).Show();
    }

    public virtual bool CanShowRegisteredDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowSkippedNumbers(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Reports.GetSkippedNumbersReport();
      report.DocumentRegister = _obj;
      report.Open();
    }

    public virtual bool CanShowSkippedNumbers(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged;
    }

    public virtual void ShowDocumentRegister(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.DocumentFlow == DocumentFlow.Incoming)
      {
        var report = RecordManagement.Reports.GetIncomingDocumentsReport();
        if (report != null)
        {
          report.DocumentRegister = _obj;
          report.Open();
        }
      }
      else if (_obj.DocumentFlow == DocumentFlow.Outgoing)
      {
        var report = RecordManagement.Reports.GetOutgoingDocumentsReport();
        if (report != null)
        {
          report.DocumentRegister = _obj;
          report.Open();
        }
      }
      else if (_obj.DocumentFlow == DocumentFlow.Inner)
      {
        var report = RecordManagement.Reports.GetInternalDocumentsReport();
        if (report != null)
        {
          report.DocumentRegister = _obj;
          report.Open();
        }
      }
    }

    public virtual bool CanShowDocumentRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged && _obj.DocumentFlow.Value != DocumentFlow.Contracts;
    }

    public virtual void SetNextNumber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Тип разреза нумерации журнала.
      var registerHasLeadingDocumentSection = _obj.NumberingSection == DocumentRegister.NumberingSection.LeadingDocument;
      var registerHasDepartmentSection = _obj.NumberingSection == DocumentRegister.NumberingSection.Department;
      var registerHasBusinessUnitSection = _obj.NumberingSection == DocumentRegister.NumberingSection.BusinessUnit;
      var registerHasNoNumberingSection = _obj.NumberingSection == DocumentRegister.NumberingSection.NoSection;
      
      // Для журналов без разреза нумерации сразу получить очередной номер.
      int? currentNumber = null;
      int? nextNumber = null;
      if (registerHasNoNumberingSection)
      {
        currentNumber = Functions.DocumentRegister.Remote.GetCurrentNumber(_obj, Calendar.UserToday);
        nextNumber = currentNumber + 1;
      }      
      // Диалог и параметры.
      var dialog = Dialogs.CreateInputDialog(DocumentRegisters.Resources.EnterNewNextNumber, DocumentRegisters.Resources.NewNextNumberRule);
      var leadingDocument = dialog.AddSelect(DocumentRegisters.Resources.LeadingDocument, registerHasLeadingDocumentSection, OfficialDocuments.Null);
      if (registerHasLeadingDocumentSection)
      {
        var availableLeadingDocuments = Docflow.Functions.Module.Remote.GetAvaliableLeadingDocuments();
        leadingDocument.From(availableLeadingDocuments);
      }
      var department = dialog.AddSelect(DocumentRegisters.Resources.Department, registerHasDepartmentSection, Departments.Null);
      var businessUnit = dialog.AddSelect(DocumentRegisters.Resources.BusinessUnit, registerHasBusinessUnitSection, BusinessUnits.Null);
      var newNextNumber = dialog.AddInteger(DocumentRegisters.Resources.NextNumber, true, nextNumber);
      
      // Параметры для ведущего документа, подразделения и НОР доступны, когда в журнале есть соответствующий разрез.
      leadingDocument.IsVisible = registerHasLeadingDocumentSection;
      department.IsVisible = registerHasDepartmentSection;
      businessUnit.IsVisible = registerHasBusinessUnitSection;
      
      // Переполучить номер при изменении ведущего документа.
      leadingDocument.SetOnValueChanged((x) =>
                                        {
                                          if (x.NewValue == null)
                                          {
                                            newNextNumber.Value = null;
                                          }
                                          else
                                          {
                                            currentNumber = Functions.DocumentRegister.Remote.GetCurrentNumber(_obj, Calendar.UserToday, x.NewValue.Id, 0, 0);
                                            newNextNumber.Value = currentNumber + 1;
                                          }
                                        });
      
      // Переполучить номер при изменении подразделения.
      department.SetOnValueChanged((x) =>
                                   {
                                     if (x.NewValue == null)
                                     {
                                       newNextNumber.Value = null;
                                     }
                                     else
                                     {
                                       currentNumber = Functions.DocumentRegister.Remote.GetCurrentNumber(_obj, Calendar.UserToday, 0, x.NewValue.Id, 0);
                                       newNextNumber.Value = currentNumber + 1;
                                     }
                                   });
      
      // Переполучить номер при изменении НОР.
      businessUnit.SetOnValueChanged((x) =>
                                     {
                                       if (x.NewValue == null)
                                       {
                                         newNextNumber.Value = null;
                                       }
                                       else
                                       {
                                         currentNumber = Functions.DocumentRegister.Remote.GetCurrentNumber(_obj, Calendar.UserToday, 0, 0, x.NewValue.Id);
                                         newNextNumber.Value = currentNumber + 1;
                                       }
                                     });
      
      // Установить очередной номер.
      if (dialog.Show() == DialogButtons.Ok)
      {
        if (newNextNumber.Value >= 1)
        {
          var leadingDocumentId = 0;
          if (leadingDocument.Value != null)
            leadingDocumentId = leadingDocument.Value.Id;
          
          var departmentId = 0;
          if (department.Value != null)
            departmentId = department.Value.Id;
          
          var businessUnitId = 0;
          if (businessUnit.Value != null)
            businessUnitId = businessUnit.Value.Id;
          
          var newCurrentNumber = newNextNumber.Value - 1;
          
          Functions.DocumentRegister.Remote.SetCurrentNumber(_obj, (int)newCurrentNumber, leadingDocumentId, departmentId, businessUnitId, Calendar.UserToday);
          if (currentNumber != newCurrentNumber)
            Dialogs.NotifyMessage(DocumentRegisters.Resources.NewNextNumberSet);
          else
            Dialogs.NotifyMessage(DocumentRegisters.Resources.NewNextNumberNotSet);
        }
        else
          Dialogs.ShowMessage(DocumentRegisters.Resources.WrongNextNumberError, MessageType.Error);
      }
    }

    public virtual bool CanSetNextNumber(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged && _obj.AccessRights.CanUpdate() &&
        (_obj.RegisterType == RegisterType.Numbering ||
         (_obj.RegistrationGroup != null && Functions.Module.CalculateParams(e, _obj, true)));
    }
  }
}