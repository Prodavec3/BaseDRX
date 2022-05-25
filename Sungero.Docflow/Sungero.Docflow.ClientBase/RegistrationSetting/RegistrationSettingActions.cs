using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.RegistrationSetting;

namespace Sungero.Docflow.Client
{
  
  public partial class RegistrationSettingActions
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

    public virtual void ShowDuplicate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.RegistrationSetting.Remote.GetDoubleSettings(_obj);
      
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(RegistrationSettings.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }
  
  internal static class RegistrationSettingStaticActions
  {
    public static void ShowRegistrationSettingsList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Reports.GetRegistrationSettingReport().Open();
    }

    public static bool CanShowRegistrationSettingsList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }
  }
}