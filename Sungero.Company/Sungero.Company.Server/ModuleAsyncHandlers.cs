using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Server
{
  public class ModuleAsyncHandlers
  {

    /// <summary>
    /// Обновить имя сотрудника из персоны.
    /// </summary>
    /// <param name="args">Параметры вызова асинхронного обработчика.</param>
    public virtual void UpdateEmployeeName(Sungero.Company.Server.AsyncHandlerInvokeArgs.UpdateEmployeeNameInvokeArgs args)
    {
      int personId = args.PersonId;
      Logger.DebugFormat("UpdateEmployeeName: start update employee name. Person id: {0}.", personId);
      var employees = Company.Employees.GetAll(x => x.Person.Id == personId);
      
      if (!employees.Any())
      {
        Logger.DebugFormat("UpdateEmployeeName: employee not found. Person id: {0}.", personId);
        return;
      }
      
      foreach (var employee in employees)
      {
        try
        {
          Company.Functions.Employee.UpdateName(employee, employee.Person);
          employee.Save();
        }
        catch
        {
          Logger.DebugFormat("UpdateEmployeeName: could not update name. Employee id: {0}.", employee.Id);
          args.Retry = true;
          continue;
        }
        Logger.DebugFormat("UpdateEmployeeName: name updated successfully. Employee id: {0}. Person id: {1}.", employee.Id, personId);
      }
      
    }
    
  }
}