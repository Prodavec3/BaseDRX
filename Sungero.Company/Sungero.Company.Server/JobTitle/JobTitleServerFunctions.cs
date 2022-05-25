using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.JobTitle;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  partial class JobTitleFunctions
  {
    
    /// <summary>
    /// Получить дубли должности.
    /// </summary>
    /// <returns>Список дублей должности.</returns>
    [Remote(IsPure = true)]
    public virtual List<IJobTitle> GetDuplicates()
    {
      return JobTitles.GetAll()
        .Where(x => (x.Name.Trim() == _obj.Name.Trim()) && Equals(x.Department, _obj.Department) && !Equals(x, _obj))
        .ToList();
    }
    
    /// <summary>
    /// Получить сотрудников с такой же должностью, но другим подразделением.
    /// </summary>
    /// <returns>Список сотрудников с такой же должностью, но другим подразделением.</returns>
    [Remote(IsPure = true)]
    public virtual List<IEmployee> GetEmployeesWithSameJobTitle()
    {
      return Employees.GetAll().Where(x => Equals(x.JobTitle, _obj) && !Equals(x.Department, _obj.Department)).ToList();
    }

  }
}