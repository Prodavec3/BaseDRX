using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.JobTitle;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Client
{
  partial class JobTitleActions
  {
    public virtual void ShowEmployeesWithSameJobTitle(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var employees = Functions.JobTitle.Remote.GetEmployeesWithSameJobTitle(_obj);
      if (employees.Any())
        employees.Show();
    }

    public virtual bool CanShowEmployeesWithSameJobTitle(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.JobTitle.Remote.GetDuplicates(_obj);
      if (duplicates.Any())
        duplicates.Show();
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }
  }
}