using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PowerOfAttorney;

namespace Sungero.Docflow.Server
{
  partial class PowerOfAttorneyFunctions
  {
    
    /// <summary>
    /// Получить действующие доверенности.
    /// </summary>
    /// <param name="employee">Сотрудник, которому выданы доверенности.</param>
    /// <param name="date">Дата, на которую нужна доверенность.</param>
    /// <returns>Доверенности.</returns>
    [Public, Remote(IsPure = true)]
    public static List<IPowerOfAttorney> GetActivePowerOfAttorneys(Company.IEmployee employee, DateTime? date)
    {
      return PowerOfAttorneys
        .GetAll(p => Equals(p.IssuedTo, employee) && (p.RegistrationDate == null || p.RegistrationDate <= date) && date <= p.ValidTill &&
                (p.LifeCycleState == LifeCycleState.Draft || p.LifeCycleState == LifeCycleState.Active))
        .ToList();
    }
  }
}